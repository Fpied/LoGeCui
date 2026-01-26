using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LoGeCuiShared.Services;

namespace LoGeCuiMobile.Services
{
    public static class SupabaseConfig
    {
        public static string Url { get; } = ConfigurationHelper.GetSupabaseUrl();
        public static string Key { get; } = ConfigurationHelper.GetSupabaseKey();
    }
}
