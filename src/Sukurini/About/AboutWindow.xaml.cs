using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Sukurini.About;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        SetVersionText();
    }

    private void SetVersionText()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ver = assembly.GetName().Version;
        string versionStr = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";

        var infoVersionAttr = (AssemblyInformationalVersionAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyInformationalVersionAttribute));
        if (infoVersionAttr != null && !string.IsNullOrEmpty(infoVersionAttr.InformationalVersion))
        {
            versionStr = infoVersionAttr.InformationalVersion;
        }

        VersionTextBlock.Text = $"버전 {versionStr} (Win-x64)";

        var copyrightAttr = (AssemblyCopyrightAttribute?)Attribute.GetCustomAttribute(
            assembly, typeof(AssemblyCopyrightAttribute));
        if (copyrightAttr != null && !string.IsNullOrEmpty(copyrightAttr.Copyright))
        {
            CopyrightTextBlock.Text = copyrightAttr.Copyright;
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
