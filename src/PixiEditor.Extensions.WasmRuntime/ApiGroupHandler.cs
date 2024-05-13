﻿using PixiEditor.Extensions.FlyUI.Elements;
using PixiEditor.Extensions.WasmRuntime.Management;

namespace PixiEditor.Extensions.WasmRuntime;

// This is a "dummy" class, all properties and methods are never actually used or set, it is used to tell code generators the implementation of the API
// Compiler will convert all functions with [ApiFunction] attribute to an actual WASM linker function
internal class ApiGroupHandler
{
    public ExtensionServices Api { get; }
    protected LayoutBuilder LayoutBuilder { get; }
    protected ObjectManager NativeObjectManager { get; }
}
