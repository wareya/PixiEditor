﻿using System.Runtime.InteropServices;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace PixiEditor.Api.CGlueMSBuild;

public class CApiGenerator
{
    private string InteropCContent { get; }
    private Action<string> Log { get; }
    public CApiGenerator(string interopCContent, Action<string> log)
    {
        InteropCContent = interopCContent;
        Log = log;
    }

    public string Generate(AssemblyDefinition assembly, string directory)
    {
        Log($"Reference assembly: {assembly.FullName}");
        var assemblies = LoadAssemblies(assembly, directory);

        var types = assemblies.SelectMany(a => a.MainModule.Types).ToArray();

        var importedMethods = GetImportedMethods(types);

        var exportedMethods = GetExportedMethods(types);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine();
        sb.AppendLine("/* ----AUTOGENERATED---- */");
        sb.AppendLine();

        sb.Append(GenerateImports(importedMethods));
        sb.AppendLine(GenerateExports(exportedMethods));

        sb.AppendLine(GenerateAttachImportedFunctions(importedMethods));

        return InteropCContent.Replace("void attach_imported_functions(){}", sb.ToString());
    }

    public static MethodDefinition[] GetExportedMethods(TypeDefinition[] types)
    {
        var exportedMethods = types
            .SelectMany(t => t.Methods)
            .Where(m => m.IsStatic && m.CustomAttributes.Any(a => a.AttributeType.FullName == "PixiEditor.Extensions.Wasm.ApiExportAttribute"))
            .ToArray();
        return exportedMethods;
    }

    public static MethodDefinition[] GetImportedMethods(TypeDefinition[] types)
    {
        var importedMethods = types
            .SelectMany(t => t.Methods)
            .Where(m => m.IsStatic && m.ImplAttributes == MethodImplAttributes.InternalCall)
            .ToArray();
        return importedMethods;
    }

    public List<AssemblyDefinition> LoadAssemblies(AssemblyDefinition assembly, string directory)
    {
        var assemblies = assembly.MainModule.AssemblyReferences
            .Where(r => !r.Name.StartsWith("System") && !r.Name.StartsWith("Microsoft"))
            .Select(x =>
            {
                Log($"Loading assembly from {directory}/{x.Name}.dll");
                return AssemblyDefinition.ReadAssembly(Path.Combine(directory, x.Name + ".dll"));
            })
            .ToList();

        assemblies.Add(assembly);
        return assemblies;
    }

    public string GenerateImports(MethodDefinition[] importedMethods)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var method in importedMethods)
        {
            sb.AppendLine(BuildImportFunction(method));
            sb.AppendLine(BuildAttachedFunction(method));
        }

        return sb.ToString();
    }

    public string GenerateExports(MethodDefinition[] exportedMethods)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var method in exportedMethods)
        {
            sb.AppendLine(BuildExportFunction(method));
        }

        return sb.ToString();
    }

    public string GenerateAttachImportedFunctions(MethodDefinition[] importedMethods)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("void attach_imported_functions()");
        sb.AppendLine("{");
        foreach (var method in importedMethods)
        {
            sb.AppendLine(GenerateAttachImportedFunction(method));
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string BuildExportFunction(MethodDefinition method)
    {
        string exportName = method.CustomAttributes.First(a => a.AttributeType.FullName == "PixiEditor.Extensions.Wasm.ApiExportAttribute").ConstructorArguments[0].Value.ToString();
        StringBuilder sb = new StringBuilder();
        sb.Append($"__attribute__((export_name(\"{exportName}\")))");
        sb.AppendLine();
        BuildMethodDeclaration(method.ReturnType, sb, exportName, method.Parameters, false);
        sb.AppendLine("{");
        sb.AppendLine($"MonoMethod* method = lookup_interop_method(\"{method.Name}\");");
        string[] paramsToPass = CParamsToMonoVars(method.Parameters, sb);
        if (paramsToPass.Length > 0)
        {
            sb.AppendLine($"void* args[] = {{{string.Join(", ", paramsToPass)}}};");
            sb.AppendLine("invoke_interop_method(method, args);");
        }
        else
        {
            sb.AppendLine("invoke_interop_method(method, NULL);");
        }

        sb.AppendLine("free(method);");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string BuildImportFunction(MethodDefinition method)
    {
        string functionName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($@"__attribute__((import_name(""{functionName}"")))");

        BuildMethodDeclaration(returnType, sb, functionName, parameters, true);

        sb.Append(";");

        return sb.ToString();
    }

    private static void BuildMethodDeclaration(TypeReference returnType, StringBuilder sb, string functionName,
        Collection<ParameterDefinition> parameters, bool extractLength)
    {
        string returnTypeMapped = TypeMapper.MapToCType(returnType);
        sb.Append($"{returnTypeMapped} {functionName}(");

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            string parameterTypeMapped = string.Join(", ", TypeMapper.MapToCTypeParam(parameter.ParameterType, parameter.Name, extractLength));
            sb.Append(parameterTypeMapped);
            if (i < parameters.Count - 1)
            {
                sb.Append(", ");
            }
        }

        sb.AppendLine(")");
    }

    private string BuildAttachedFunction(MethodDefinition method)
    {
        StringBuilder sb = new StringBuilder();
        string functionName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;

        string returnTypeMapped = TypeMapper.MapToCType(returnType);
        sb.Append($"{returnTypeMapped} internal_{functionName}(");

        BuildMonoParams(parameters, sb);
        sb.AppendLine(")");
        sb.AppendLine("{");
        BuildCBody(method, sb);
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void BuildMonoParams(Collection<ParameterDefinition> parameters, StringBuilder sb)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            string parameterTypeMapped = TypeMapper.MapToMonoTypeParam(parameter.ParameterType, parameter.Name);
            sb.Append(parameterTypeMapped);
            if (i < parameters.Count - 1)
            {
                sb.Append(", ");
            }
        }
    }

    private static void BuildCBody(MethodDefinition method, StringBuilder sb)
    {
        string[] paramsToPass = MonoParamsToCVars(method.Parameters, sb);
        BuildInvokeImport(method, sb, paramsToPass);
    }

    private static string[] MonoParamsToCVars(Collection<ParameterDefinition> methodParameters, StringBuilder sb)
    {
        List<string> cVars = new List<string>();
        for (int i = 0; i < methodParameters.Count; i++)
        {
            var parameter = methodParameters[i];
            if(TypeMapper.RequiresConversion(parameter.ParameterType))
            {
                string varName = $"c_{parameter.Name}";
                ConvertedParam[] convertedParams = TypeMapper.ConvertMonoToCType(parameter.ParameterType, parameter.Name, varName);

                foreach (var convertedParam in convertedParams)
                {
                    sb.AppendLine(convertedParam.FullExpression);
                    cVars.Add(convertedParam.VarName);
                }
            }
            else
            {
                cVars.Add(parameter.Name);
            }
        }

        return cVars.ToArray();
    }

    private string[] CParamsToMonoVars(Collection<ParameterDefinition> methodParameters, StringBuilder sb)
    {
        List<string> monoVars = new List<string>();
        for (int i = 0; i < methodParameters.Count; i++)
        {
            var parameter = methodParameters[i];
            if(TypeMapper.RequiresConversion(parameter.ParameterType))
            {
                string varName = $"mono_{parameter.Name}";
                ConvertedParam[] convertedParams = TypeMapper.CToMonoType(parameter.ParameterType, parameter.Name, varName);

                foreach (var convertedParam in convertedParams)
                {
                    sb.AppendLine(convertedParam.FullExpression);
                    string varToPass = !convertedParam.IsPointer ? $"&{convertedParam.VarName}" : convertedParam.VarName;
                    monoVars.Add(varToPass);
                }
            }
            else
            {
                monoVars.Add($"&{parameter.Name}"); // TODO: make sure appending pointer is always correct
            }
        }

        return monoVars.ToArray();
    }

    private static void BuildInvokeImport(MethodDefinition method, StringBuilder sb, string[] paramsToPass)
    {
        string functionName = method.Name;
        if (method.ReturnType.FullName != "System.Void")
        {
            sb.Append("return ");
        }
        sb.Append($"{functionName}(");
        sb.Append(string.Join(", ", paramsToPass));
        sb.AppendLine(");");
    }

    private string GenerateAttachImportedFunction(MethodDefinition method)
    {
        StringBuilder sb = new StringBuilder();
        string functionName = method.Name;
        string funcNamespace = method.DeclaringType.FullName;
        sb.Append($"mono_add_internal_call(\"{funcNamespace}::{functionName}\", internal_{functionName});");
        return sb.ToString();
    }
}

public class ConvertedParam
{
    public string VarName { get; set; }
    public string VarType { get; set; }
    public string ConversionString { get; set; }
    public string FullExpression => $"{VarType} {VarName} = {ConversionString};";

    public bool IsPointer => VarType.EndsWith("*");

    public ConvertedParam(string varName, string varType, string conversionString)
    {
        VarName = varName;
        VarType = varType;
        ConversionString = conversionString;
    }
}
