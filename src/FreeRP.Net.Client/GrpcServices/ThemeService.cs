using Microsoft.FluentUI.AspNetCore.Components.DesignTokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.GrpcServices
{
    public class ThemeService(BaseLayerLuminance baseLayerLuminance, AccentBaseColor accentBaseColor)
    {
        private readonly BaseLayerLuminance _baseLayerLuminance = baseLayerLuminance;
        private readonly AccentBaseColor _accentBaseColor = accentBaseColor;
    }
}
