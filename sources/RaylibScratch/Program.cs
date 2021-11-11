// See https://aka.ms/new-console-template for more information


using RaylibScratch;

Console.WriteLine("Main thread start");


var renderSystem = new RenderSystem();
renderSystem.Start();
await renderSystem.renderTask;

//renderSystem._RenderThread_Worker();


Console.WriteLine("Main thread done");

