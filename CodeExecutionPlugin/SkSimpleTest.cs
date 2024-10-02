//using Microsoft.SemanticKernel.Connectors.OpenAI;
//using Microsoft.SemanticKernel;
//using CodeExecutionPlugin.plugins;

//namespace CodeExecutionPlugin;

//public class SkSimpleTest
//{
//    public async Task Execute()
//    {
//        var modelDeploymentName = "gpt4";
//        var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AzureOpenAI_Endpoint", EnvironmentVariableTarget.User);
//        var azureOpenAIApiKey = Environment.GetEnvironmentVariable("AzureOpenAI_ApiKey", EnvironmentVariableTarget.User);

//        var builder = Kernel.CreateBuilder();
//        builder.Services.AddAzureOpenAIChatCompletion(
//            modelDeploymentName,
//            azureOpenAIEndpoint,
//            azureOpenAIApiKey,
//            modelId: "gpt-4"
//        );
//        //builder.Plugins.AddFromType<WhatTimeIsIt>();
//        var kernel = builder.Build();

//        // Also able to add it after the kernel has been built

//        kernel.ImportPluginFromType<WhatDateIsIt>();

//        string userPrompt = "I would like to know what date is it and 5 significative" +
//            "things that happened on the past on this day.";

//        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
//        {
//            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
//        };

//        var result = await kernel.InvokePromptAsync(
//            userPrompt,
//            new(openAIPromptExecutionSettings));

//        Console.WriteLine($"Result: {result}");
//        Console.WriteLine();
//    }
//}
