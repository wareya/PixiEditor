using Avalonia.Controls;
using Avalonia.Interactivity;
using PixiEditor.Extensions.CommonApi.LayoutBuilding.Events;
using PixiEditor.Extensions.LayoutBuilding;
using PixiEditor.Extensions.LayoutBuilding.Elements;
using Button = PixiEditor.Extensions.LayoutBuilding.Elements.Button;

namespace PixiEditor.Extensions.Test;

public class LayoutBuilderTests
{
    [Fact]
    public void TestCenteredTextLayoutIsBuildCorrectly()
    {
        Layout layout = new Layout(
            body: new Center(
                child: new Text("Hello")));

        object result = layout.Build();

        Assert.IsType<Panel>(result);
        Panel grid = (Panel)result;
        Assert.Single(grid.Children);

        Assert.IsType<Panel>(grid.Children[0]);
        Panel childGrid = (Panel)grid.Children[0];

        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center, childGrid.HorizontalAlignment);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Center, childGrid.VerticalAlignment);

        Assert.Single(childGrid.Children);

        Assert.IsType<TextBlock>(childGrid.Children[0]);
        TextBlock textBlock = (TextBlock)childGrid.Children[0];

        Assert.Equal("Hello", textBlock.Text);
    }

    [Fact]
    public void TestThatButtonClickEventFiresCallback()
    {
        Button button = new Button();
        bool callbackFired = false;

        button.Click += (e) => callbackFired = true;
        button.RaiseEvent(nameof(Button.Click), ElementEventArgs.Empty);

        Assert.True(callbackFired);
    }

    [Fact]
    public void TestThatAvaloniaClickEventFiresElementCallback()
    {
        Button button = new Button();
        bool callbackFired = false;

        button.Click += (e) => callbackFired = true;

        button.Build().RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));

        Assert.True(callbackFired);
    }
}
