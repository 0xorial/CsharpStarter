using System.Diagnostics;
using Flurl.Http;
using Scaffold.Tests.Utils;

namespace Scaffold.Tests;

[TestClass]
public class TestAssemblyInitializer
{
    [AssemblyInitialize]
    public static async Task AssemblyInitialize(TestContext context)
    {
        TestHelper.EnsureLocalDbExistsAndMigrateUp();

        // we don't care about the correctness of time during debugging
        // and it makes it faster and easier to avoid warmup
        // (in particular because services don't get started/stopped)
        if (!Debugger.IsAttached)
        {
            // warmup backend to have a consistent timing in individual tests
            // (otherwise first tests will take longer than the rest, depending on the order in which they ran)
            using var h = await TestHelper.Create();
            await (await h.GetClient().Request("list").GetAsync()).GetStringAsync();
        }
    }
}