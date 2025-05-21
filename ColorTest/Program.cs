using Spectre.Console; foreach (var prop in typeof(Color).GetFields()) { if (prop.IsStatic && prop.IsPublic) { Console.WriteLine($"{prop.Name}"); } }
