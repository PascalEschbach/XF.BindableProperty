using System;
using Fody;
using Xunit;

//TODO: Write tests
public class BindableTests {

    private static TestResult __testResult;

    private dynamic Instance => (dynamic)Activator.CreateInstance( __testResult.Assembly.GetType( "TestClass" ) );


    static BindableTests() {
        var weavingTask = new ModuleWeaver();
        __testResult = weavingTask.ExecuteTestRun( "AssemblyToProcess.dll", runPeVerify: false );
    }


    [Fact]
    public void Validate_Default_Value_Is_Set() {
        Assert.Equal( "This is a test", Instance.Auto );
    }
}
