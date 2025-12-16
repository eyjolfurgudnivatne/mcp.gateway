namespace NotificationMcpServerTests.Fixture;

/// <summary>
/// Collection definition for shared test fixture across all notification tests.
/// </summary>
[CollectionDefinition("ServerCollection")]
public class ServerCollectionDefinition : ICollectionFixture<NotificationMcpServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
