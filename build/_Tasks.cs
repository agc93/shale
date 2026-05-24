using System.Reflection;
public interface ITaskModule
{
    int Order => 0;
    void Register(Func<string, CakeTaskBuilder> task, ICakeContext context, BuildConfiguration state);
    List<string> RequiredTools { get; }
}
public static class TaskModuleLoader
{
    public static void RegisterAll(
        this ICakeContext context,
        Func<string, CakeTaskBuilder> task,
        Func<string, FilePath[]> toolInstaller,
        BuildConfiguration state)
    {
        var modules = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                typeof(ITaskModule).IsAssignableFrom(t) &&
                t is { IsInterface: false, IsAbstract: false } &&
                t.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(t => t.Name)
            .Select(t => (ITaskModule)Activator.CreateInstance(t)!);

        foreach (var module in modules)
        {
            context.Information($"Registering task module: {module.GetType().Name}");
            module.Register(task, context, state);
            foreach (var tool in module.RequiredTools) {
                context.Information($"Installing '{tool}'...");
                toolInstaller(tool);
            }
        }
    }
}