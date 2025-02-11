using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=app.db"));
builder.Services.AddScoped<IClientsRepository, ClientsRepository>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.MapGet("/", () => "Hello World!");
app.MapGet("/orders", async (IOrdersRepository ordersRepository) =>
{
    var orders = await ordersRepository.GetAllAsync();
    return Results.Ok(orders);
});
app.MapPost("/clients", async ([FromBody] Client client, [FromServices] IClientService clientService) =>
{
    try
    {
        await clientService.CreateClientAsync(client);
        return Results.Created($"/clients/{client.Id}", client);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/orders", async ([FromBody] Order order, [FromServices] IOrderService orderService) =>
{
    try
    {
        await orderService.CreateOrderAsync(order);
        return Results.Created($"/orders/{order.Id}", order);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPut("/clients/{id:int}", async (int id, [FromBody] Client updatedClient, [FromServices] IClientService clientService) =>
{
    try
    {
        var existingClient = await clientService.GetClientByPhoneNumberAsync(updatedClient.PhoneNumber);

        if (existingClient is not null && existingClient.Id != id)
        {
            return Results.Conflict($"Клиент с таким номером телефона уже существует.");
        }

        existingClient = await clientService.GetClientByPhoneNumberAsync(existingClient?.PhoneNumber ?? "");

        if (existingClient is null)
        {
            return Results.NotFound($"Клиент с идентификатором {id} не найден.");
        }

        existingClient.FirstName = updatedClient.FirstName;
        existingClient.LastName = updatedClient.LastName;
        existingClient.Email = updatedClient.Email;

        await clientService.UpdateClientAsync(existingClient);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
app.Run();




public class AppDbContext : DbContext
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=app.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}

public interface IClientsRepository
{
    Task<List<Client>> GetAllAsync();
    Task<Client?> FindByIdAsync(int id);
    Task<Client?> FindByPhoneNumberAsync(string phoneNumber);
    Task AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task RemoveAsync(Client client);
}

public class ClientsRepository : IClientsRepository
{
    private readonly AppDbContext _context;

    public ClientsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Client>> GetAllAsync()
    {
        return await _context.Clients.ToListAsync();
    }

    public async Task<Client?> FindByIdAsync(int id)
    {
        return await _context.Clients.FindAsync(id);
    }

    public async Task<Client?> FindByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Clients.SingleOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
    }

    public async Task AddAsync(Client client)
    {
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Client client)
    {
        _context.Entry(client).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Client client)
    {
        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
    }
}

public interface IClientService
{
    Task<Client> CreateClientAsync(Client client);
    Task<Client?> GetClientByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<Client>> GetAllClientsAsync();
    Task DeleteClientAsync(string phoneNumber);
    Task UpdateClientAsync(Client client);
}

public class ClientService : IClientService
{
    private readonly IClientsRepository _repository;

    public ClientService(IClientsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Client> CreateClientAsync(Client client)
    {
        await _repository.AddAsync(client);
        return client;
    }

    public async Task<Client?> GetClientByPhoneNumberAsync(string phoneNumber)
    {
        return await _repository.FindByPhoneNumberAsync(phoneNumber);
    }

    public async Task<IEnumerable<Client>> GetAllClientsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task DeleteClientAsync(string phoneNumber)
    {
        var client = await _repository.FindByPhoneNumberAsync(phoneNumber);
        if (client != null)
        {
            await _repository.RemoveAsync(client);
        }
    }
    public async Task UpdateClientAsync(Client client)
    {
        await _repository.UpdateAsync(client);
    }

}

public interface IOrdersRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> FindByIdAsync(int id);
    Task<Order?> FindByClientAsync(Client client);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task RemoveAsync(Order order);
}

public class OrdersRepository : IOrdersRepository
{
    private readonly AppDbContext _context;

    public OrdersRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders.ToListAsync();
    }

    public async Task<Order?> FindByIdAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order?> FindByClientAsync(Client client)
    {
        return await _context.Orders.SingleOrDefaultAsync(o => o.Client == client);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Order order)
    {
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
    }
}

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(int id);
    Task<IEnumerable<Order>> GetAllOrdersAsync();
    Task DeleteOrderAsync(int id);
}

public class OrderService : IOrderService
{
    private readonly IOrdersRepository _repository;

    public OrderService(IOrdersRepository repository)
    {
        _repository = repository;
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        await _repository.AddAsync(order);
        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _repository.FindByIdAsync(id);
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task DeleteOrderAsync(int id)
    {
        var order = await _repository.FindByIdAsync(id);
        if (order != null)
        {
            await _repository.RemoveAsync(order);
        }
    }
}

public class Client
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    private string _phoneNumber = "";
    public string PhoneNumber
    {
        get { return _phoneNumber; }
        set
        {
            ValidatePhoneNumber(value);
            _phoneNumber = value;
        }
    }
    public string? Email { get; set; }

    public Client(string firstName, string lastName, string phoneNumber, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Email = email;
    }

    private void ValidatePhoneNumber(string phoneNumber)
    {
        foreach (char symbol in phoneNumber)
        {
            if (char.IsLetter(symbol))
                throw new Exception("Неверный формат номера телефона. Номер должен содержать только цифры.");
        }
    }
}

public class Executor
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    private string _phoneNumber = "";
    public string PhoneNumber
    {
        get { return _phoneNumber; }
        set
        {
            ValidatePhoneNumber(value);
            _phoneNumber = value;
        }
    }

    public Executor(string firstName, string lastName, string phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    private void ValidatePhoneNumber(string phoneNumber)
    {
        foreach (char symbol in phoneNumber)
        {
            if (!char.IsDigit(symbol))
                throw new Exception("Неверный формат номера телефона. Номер должен содержать только цифры.");
        }
    }
}

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Device { get; set; }
    public string ProblemType { get; set; }
    public string Description { get; set; }
    public OrderStatus Status { get; set; }
    public Client? Client { get; set; }
    public Executor? Executor { get; set; }

    public Order(DateTime orderDate, string device, string problemType, string description, OrderStatus status, Client client, Executor executor)
    {
        OrderDate = orderDate;
        Device = device;
        ProblemType = problemType;
        Description = description;
        Status = status;
        Client = client;
        Executor = executor;
    }

    public void ChangeStatus()
    {
        switch (Status)
        {
            case OrderStatus.WaitingForExecution:
                Status = OrderStatus.InRepair;
                break;
            case OrderStatus.InRepair:
                Status = OrderStatus.ReadyToIssue;
                break;
            case OrderStatus.ReadyToIssue:
                Status = OrderStatus.WaitingForExecution;
                break;
        }
    }
}

public enum OrderStatus
{
    WaitingForExecution,
    InRepair,
    ReadyToIssue
}