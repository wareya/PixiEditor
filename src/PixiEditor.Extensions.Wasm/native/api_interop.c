#include <mono-wasi/driver.h>
#include <string.h>
#include <assert.h>

MonoMethod* method_entry;

__attribute__((import_name("log_message")))
void log_message(const char* message);

__attribute__((export_name("entry")))
void entry()
{
    if (!method_entry) {
        method_entry = lookup_dotnet_method("PixiEditor.Extensions.Wasm.dll", "PixiEditor.Extensions.Wasm", "Interop", "Entry", -1);
        assert(method_entry);
    }

    void* method_params[] = {};
    MonoObject *exception;
    mono_wasm_invoke_method (method_entry, NULL, method_params, &exception);
    assert (!exception);
}

void logger_log_message(MonoString* message)
{
    char* message_utf8 = mono_wasm_string_get_utf8(message);
    log_message(message_utf8);
}

void attach_internal_calls()
{
    mono_add_internal_call("PixiEditor.Extensions.Wasm.Interop::LogMessage", logger_log_message);
}