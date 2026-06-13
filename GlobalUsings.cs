global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Text;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Input;
global using System.Windows.Media;
global using System.Windows.Threading;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Spur.Interop;
global using Spur.Animations;
global using Spur.Models;
global using Spur.Services;
// B1: Alias so callers can use ISpurLogger to avoid shadowing
// Microsoft.Extensions.Logging.ILogger if that namespace is ever imported.
global using ISpurLogger = Spur.Services.ILogger;
