// See https://aka.ms/new-console-template for more information
using CodeExecutionPlugin;

Console.WriteLine("Hello, SK World!");

await DotNetInteractiveTestV01.Execute();
await DotNetInteractiveTest.Execute();

//SkSimpleTest simpleSK = new();
//await simpleSK.Execute();