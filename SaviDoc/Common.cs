using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Microsoft.Extensions.Configuration;
using Savian.SaviDoc.SupportClasses.Configuration;
using static Savian.SaviDoc.SasProgram;
using static Savian.SaviDoc.Program;

namespace Savian.SaviDoc
{
    internal static class Common
    {
        internal static IConfiguration Configuration { get; set; }
        internal static CommonConfigurationType CommonConfiguration { get; set; } = new CommonConfigurationType();

        internal static void Initialize(Options options)
        {
            if (!string.IsNullOrEmpty(options.LogFile))
            { 
                Log.Logger = new LoggerConfiguration()
                        .WriteTo.File(options.LogFile)
                        .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();
            }

            var builder = new ConfigurationBuilder()
                  .AddJsonFile(@"appsettings.json")
                  ;
            Configuration = builder.Build();
            Configuration.GetSection("CommonConfiguration").Bind(CommonConfiguration);
        }
        internal static Regex GetPattern(string section, string name)
        {
            var tag = CommonConfiguration.Tags.FirstOrDefault(p => p.Section == section && p.Name == name);
            return tag != null ? new Regex(tag.Pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace) : new Regex("");
        }
    }
}

