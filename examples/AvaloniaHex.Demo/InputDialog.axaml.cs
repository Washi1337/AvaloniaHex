using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaHex.Demo;

public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public string? Prompt
    {
        get => PromptLabel.Content as string;
        set => PromptLabel.Content = value;
    }

    public string? Input
    {
        get => InputTextBox.Text;
        set => InputTextBox.Text = value;
    }

    public string? PlaceholderText
    {
        get => InputTextBox.PlaceholderText;
        set => InputTextBox.PlaceholderText = value;
    }

    public Predicate<string?> IsValid
    {
        get;
        set;
    } = static _ => true;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        InputTextBox.Focus();
        InputTextBox.SelectAll();
    }

    private void OKButtonOnClick(object? sender, RoutedEventArgs e) => Close(InputTextBox.Text);

    private void CancelButtonOnClick(object? sender, RoutedEventArgs e) => Close(null);

    private void InputTextBoxOnTextChanged(object? sender, TextChangedEventArgs e) => OKButton.IsEnabled = IsValid(Input);
}