using Microsoft.Maui.Controls;
using System;

namespace LoGeCuiMobile.Resources.Lang;

[ContentProperty(nameof(Key))]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = "";

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
        => new Binding($"[{Key}]", source: LocalizationResourceManager.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}