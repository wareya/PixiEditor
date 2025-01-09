﻿using System.Xml.Linq;
using PixiEditor.SVG.Units;

namespace PixiEditor.SVG;

public abstract class SvgProperty
{
    protected SvgProperty(string svgName)
    {
        SvgName = svgName;
    }
    
    protected SvgProperty(string svgName, string? namespaceName, string? namespaceUri) : this(svgName)
    {
        NamespaceName = namespaceName;
        NamespaceUri = namespaceUri;
    }

    public string? NamespaceName { get; set; }
    public string? NamespaceUri { get; set; }
    public string SvgName { get; set; }
    public ISvgUnit? Unit { get; set; }
}

public class SvgProperty<T> : SvgProperty where T : struct, ISvgUnit
{
    public new T? Unit
    {
        get => (T?)base.Unit;
        set => base.Unit = value;
    }
    
    public SvgProperty(string svgName) : base(svgName)
    {
    }
    
    public SvgProperty(string svgName, string? namespaceName, string? namespaceUri) : base(svgName, namespaceName, namespaceUri)
    {
    }
}
