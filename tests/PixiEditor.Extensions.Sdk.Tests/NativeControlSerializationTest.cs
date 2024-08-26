using System.Text;
using PixiEditor.Extensions.CommonApi.FlyUI;
using PixiEditor.Extensions.Sdk.Api.FlyUI;

namespace PixiEditor.Extensions.Sdk.Tests;

public class NativeControlSerializationTest
{
    [Fact]
    public void TestThatNoChildLayoutSerializesCorrectBytes()
    {
        CompiledControl layout = new CompiledControl(0, "Layout");
        layout.AddProperty("Title");

        int uniqueId = 0;
        byte[] uniqueIdBytes = BitConverter.GetBytes(uniqueId);

        string controlId = "Layout";
        byte[] controlIdBytes = Encoding.UTF8.GetBytes(controlId);

        int propertiesCount = 1;
        byte[] propertiesCountBytes = BitConverter.GetBytes(propertiesCount);

        int stringLen = "Title".Length;
        byte[] stringLenBytes = BitConverter.GetBytes(stringLen);

        byte[] titleBytes = Encoding.UTF8.GetBytes("Title");

        int childCount = 0;
        byte[] childCountBytes = BitConverter.GetBytes(childCount);

        List<byte> expectedBytes = new();
        expectedBytes.AddRange(uniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(controlId.Length));
        expectedBytes.AddRange(controlIdBytes);
        expectedBytes.AddRange(propertiesCountBytes);
        expectedBytes.Add(ByteMap.GetTypeByteId(typeof(string)));
        expectedBytes.AddRange(stringLenBytes);
        expectedBytes.AddRange(titleBytes);
        expectedBytes.AddRange(childCountBytes);

        Assert.Equal(expectedBytes.ToArray(), layout.Serialize().ToArray());
    }

    [Fact]
    public void TestThatChildLayoutSerializesCorrectBytes()
    {
        CompiledControl layout = new CompiledControl(0, "Layout");
        layout.AddChild(new CompiledControl(1, "Center"));

        int uniqueId = 0;
        byte[] uniqueIdBytes = BitConverter.GetBytes(uniqueId);

        string controlId = "Layout";
        byte[] controlIdBytes = Encoding.UTF8.GetBytes(controlId);

        int propertiesCount = 0;
        byte[] propertiesCountBytes = BitConverter.GetBytes(propertiesCount);

        int childCount = 1;
        byte[] childCountBytes = BitConverter.GetBytes(childCount);

        int childUniqueId = 1;
        byte[] childUniqueIdBytes = BitConverter.GetBytes(childUniqueId);

        string childControlId = "Center";
        byte[] childControlIdBytes = Encoding.UTF8.GetBytes(childControlId);

        int childPropertiesCount = 0;
        byte[] childPropertiesCountBytes = BitConverter.GetBytes(childPropertiesCount);

        int childChildCount = 0;
        byte[] childChildCountBytes = BitConverter.GetBytes(childChildCount);

        List<byte> expectedBytes = new();
        expectedBytes.AddRange(uniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(controlId.Length));
        expectedBytes.AddRange(controlIdBytes);
        expectedBytes.AddRange(propertiesCountBytes);
        expectedBytes.AddRange(childCountBytes);
        expectedBytes.AddRange(childUniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(childControlId.Length));
        expectedBytes.AddRange(childControlIdBytes);
        expectedBytes.AddRange(childPropertiesCountBytes);
        expectedBytes.AddRange(childChildCountBytes);

        Assert.Equal(expectedBytes.ToArray(), layout.Serialize().ToArray());
    }

    [Fact]
    public void TestThatChildNestedLayoutSerializesCorrectBytes()
    {
        CompiledControl layout = new CompiledControl(0, "Layout");
        CompiledControl center = new CompiledControl(1, "Center");
        CompiledControl text = new CompiledControl(2, "Text");
        text.AddProperty("Hello world");
        center.AddChild(text);
        layout.AddChild(center);

        int uniqueId = 0;
        byte[] uniqueIdBytes = BitConverter.GetBytes(uniqueId);

        string controlId = "Layout";
        byte[] controlIdBytes = Encoding.UTF8.GetBytes(controlId);

        int propertiesCount = 0;
        byte[] propertiesCountBytes = BitConverter.GetBytes(propertiesCount);

        int childCount = 1;
        byte[] childCountBytes = BitConverter.GetBytes(childCount);

        int childUniqueId = 1;
        byte[] childUniqueIdBytes = BitConverter.GetBytes(childUniqueId);

        string childControlId = "Center";
        byte[] childControlIdBytes = Encoding.UTF8.GetBytes(childControlId);

        int childPropertiesCount = 0;
        byte[] childPropertiesCountBytes = BitConverter.GetBytes(childPropertiesCount);

        int childChildCount = 1;
        byte[] childChildCountBytes = BitConverter.GetBytes(childChildCount);

        int textUniqueId = 2;
        byte[] textUniqueIdBytes = BitConverter.GetBytes(textUniqueId);

        string textControlId = "Text";
        byte[] textControlIdBytes = Encoding.UTF8.GetBytes(textControlId);

        int textPropertiesCount = 1;
        byte[] textPropertiesCountBytes = BitConverter.GetBytes(textPropertiesCount);

        int textStringLen = "Hello world".Length;
        byte[] textStringLenBytes = BitConverter.GetBytes(textStringLen);

        byte[] textTitleBytes = Encoding.UTF8.GetBytes("Hello world");

        int textChildCount = 0;
        byte[] textChildCountBytes = BitConverter.GetBytes(textChildCount);


        List<byte> expectedBytes = new();
        expectedBytes.AddRange(uniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(controlId.Length));
        expectedBytes.AddRange(controlIdBytes);
        expectedBytes.AddRange(propertiesCountBytes);
        expectedBytes.AddRange(childCountBytes);

        expectedBytes.AddRange(childUniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(childControlId.Length));
        expectedBytes.AddRange(childControlIdBytes);
        expectedBytes.AddRange(childPropertiesCountBytes);
        expectedBytes.AddRange(childChildCountBytes);

        expectedBytes.AddRange(textUniqueIdBytes);
        expectedBytes.AddRange(BitConverter.GetBytes(textControlId.Length));
        expectedBytes.AddRange(textControlIdBytes);
        expectedBytes.AddRange(textPropertiesCountBytes);
        expectedBytes.Add(ByteMap.GetTypeByteId(typeof(string)));
        expectedBytes.AddRange(textStringLenBytes);
        expectedBytes.AddRange(textTitleBytes);
        expectedBytes.AddRange(textChildCountBytes);

        Assert.Equal(expectedBytes.ToArray(), layout.Serialize().ToArray());
    }

    [Fact]
    public void TestThatLayoutBuilderProperlyConvertsToNativeControls()
    {
        Layout layout = new Layout(
            new Center(
                child: new Text("hello sexy.")));

        CompiledControl compiledControl = layout.BuildNative();

        Assert.Equal("Layout", compiledControl.ControlTypeId);
        Assert.Empty(compiledControl.Properties);
        Assert.Single(compiledControl.Children);

        Assert.Equal("Center", compiledControl.Children[0].ControlTypeId);
        Assert.Empty(compiledControl.Children[0].Properties);

        Assert.Equal("Text", compiledControl.Children[0].Children[0].ControlTypeId);
        Assert.True(compiledControl.Children[0].Children[0].Properties.Count > 0);
        Assert.Equal("hello sexy.", compiledControl.Children[0].Children[0].Properties[0].value);
    }

    [Fact]
    public void TestThatBuildButtonQueuesEvents()
    {
        Button button = new Button(
            child: new Text("hello sexy."),
            onClick: _ => { });

        button.BuildNative();

        Assert.Contains(button.BuildQueuedEvents, x => x == "Click");
    }
}
