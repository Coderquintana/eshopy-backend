using Xunit;

// WebApplicationFactory<Program> usa HostFactoryResolver internamente para interceptar Build() del
// host antes de que corra — ese mecanismo no es seguro para invocaciones concurrentes en el mismo
// proceso. xUnit corre clases de test distintas en paralelo por default; cada clase de esta suite
// crea su propia SecurityWebApplicationFactory (IClassFixture, no compartida), asi que sin esto
// varias instancias intentan resolver el host al mismo tiempo y la interceptcion falla de forma
// intermitente ("The entry point exited without ever building an IHost").
[assembly: CollectionBehavior(DisableTestParallelization = true)]
