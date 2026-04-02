using DataAccessLayer.Entities;
using DataAccessLayer.RepositoryContracts;
using MongoDB.Driver;

namespace DataAccessLayer.Repository;

public class OrdersRepository : IOrdersRepository
{
    private readonly string collectionName = "orders";
    private readonly IMongoCollection<Order> _ordersCollection;

    public OrdersRepository(IMongoDatabase mongoDatabase) {
        _ordersCollection = mongoDatabase.GetCollection<Order>(collectionName);
    }

    public async Task<Order?> AddOrder(Order order)
    {
        order.OrderID = Guid.NewGuid();
        order._id = order.OrderID;

        foreach (OrderItem orderItem in order.OrderItems)
        {
            orderItem._id = Guid.NewGuid();
        }

        await _ordersCollection.InsertOneAsync(order);

        return order;
    }

    public async Task<bool> DeleteOrder(Guid orderID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(o => o.OrderID, orderID);

        //Order? existingOrder = await (await _ordersCollection.FindAsync(filter)).FirstOrDefaultAsync();
        //if (existingOrder == null)
        //{
        //    return false;
        //}

        DeleteResult deleteResult = await _ordersCollection.DeleteOneAsync(filter);

        return deleteResult.DeletedCount > 0;
    }

    public async Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        Order? order = (await _ordersCollection.FindAsync(filter)).FirstOrDefault();

        return order;
    }

    public async Task<IEnumerable<Order>> GetOrders()
    {
        List<Order> orders = (await _ordersCollection.FindAsync(Builders<Order>.Filter.Empty)).ToList();

        return orders;
    }

    public async Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        List<Order> orders = (await _ordersCollection.FindAsync(filter)).ToList();

        return orders;
    }

    public async Task<Order?> UpdateOrder(Order order)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(o => o.OrderID, order.OrderID);

        Order? existingOrder = (await _ordersCollection.FindAsync(filter)).FirstOrDefault();
        if (existingOrder == null)
        {
            return null;
        }

        order._id = existingOrder._id;

        ReplaceOneResult replaceResult = await _ordersCollection.ReplaceOneAsync(filter, order);

        if (replaceResult.ModifiedCount == 0)
        {
            return null;
        }

        return order;
    }
}
