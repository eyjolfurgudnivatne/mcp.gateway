namespace PromptMcpServerTests.Fixture;

/// <summary>
/// -----------------
///     https://xunit.net/docs/shared-context
/// -----------------
/// 
/// Steps to use Class Fixture :
///     When you want to create a single test context and share it among all the tests in the class, 
///     and have it cleaned up after all the tests in the class have finished.
///     
/// - Create the fixture class, and put the startup code in the fixture class constructor.
/// - If the fixture class needs to perform cleanup, implement IDisposable on the fixture class, 
///     and put the cleanup code in the Dispose() method.
/// - Add IClassFixture<> to the test class.
/// - If the test class needs access to the fixture instance, 
///     add it as a constructor argument, and it will be provided automatically.
/// 
/// ------------------
/// Steps to use Collection Fixture :
///     When you want to create a single test context and share it among tests in several test classes, 
///     and have it cleaned up after all the tests in the test classes have finished.
/// 
/// - Create the fixture class, and put the startup code in the fixture class constructor.
/// - If the fixture class needs to perform cleanup, implement IDisposable on the fixture class, 
///     and put the cleanup code in the Dispose() method.
/// - Create the collection definition class, decorating it with the[CollectionDefinition] attribute, 
///     giving it a unique name that will identify the test collection.
/// - Add ICollectionFixture<> to the collection definition class.
/// - Add the[Collection] attribute to all the test classes that will be part of the collection, 
///     using the unique name you provided to the test collection definition class’s[CollectionDefinition] attribute.
/// - If the test classes need access to the fixture instance, add it as a constructor argument, 
///     and it will be provided automatically.
/// </summary>
[CollectionDefinition("ServerCollection")]
public class ServerCollectionDefinition : ICollectionFixture<PromptMcpServerFixture> //, ICollectionFixture<OtherFixture>, ICollectionFixture<AnotherFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}