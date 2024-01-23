#include <mono-wasi/driver.h>
#include <string.h>
#include <assert.h>
#include "api.h"

MonoMethod* lookup_interop_method(const char* method_name)
{
    MonoMethod* method = NULL;
    method = lookup_dotnet_method("PixiEditor.Extensions.Wasm.dll", "PixiEditor.Extensions.Wasm", "Interop", method_name, -1);
    assert(method);

    return method;
}

void invoke_interop_method(MonoMethod* method)
{
    void* method_params[] = {  };
    MonoObject* exception;
    mono_wasm_invoke_method(method, NULL, method_params, &exception);
    assert(!exception);
}

__attribute((export_name("load")))
void load()
{
    MonoMethod* metod = lookup_interop_method("Load");
    invoke_interop_method(metod);
}

__attribute((export_name("initialize")))
void initialize()
{
    MonoMethod* metod = lookup_interop_method("Initialize");
    invoke_interop_method(metod);
}

void attach_internal_calls()
{
    attach_logger_calls();
    attach_window_calls();
}