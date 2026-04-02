using DataAccessLayer.Entities;
using MongoDB.Driver;

namespace DataAccessLayer.RepositoryContracts;

public interface IOrdersRepository
{
    /// <summary>
    /// Retrieves all Orders asynchronously.
    /// </summary>
    /// <returns>Returns all orders from the orders collection</returns>
    Task<IEnumerable<Order>> GetOrders();

    /// <summary>
    /// Retrieves all orders based on the specified conditions asynchronously.
    /// </summary>
    /// <param name="filter">Filter condition</param>
    /// <returns>Returning a collection of matching orders</returns>
    Task<IEnumerable<Order?>> GetOrdersByCondition(FilterDefinition<Order> filter);


    /// <summary>
    /// Retrieves a single order based on the specified conditions asynchronously.
    /// </summary>
    /// <param name="filter">Filter condition</param>
    /// <returns>Returning a matching order</returns>
    Task<Order?> GetOrderByCondition(FilterDefinition<Order> filter);
    
    /// <summary>
    /// Adds a new order asynchronously.
    /// </summary>
    /// <param name="order">Order to be added</param>
    /// <returns>Returning the added order</returns>
    Task<Order?> AddOrder(Order order);

    /// <summary>
    /// Updates an existing order asynchronously.
    /// </summary>
    /// <param name="order">Order to be updated</param>
    /// <returns>Returning the updated order or null if not found</returns>
    Task<Order?> UpdateOrder(Order order);

    /// <summary>
    /// Deletes an order asynchronously
    /// </summary>
    /// <param name="orderID">Order ID of order to be deleted</param>
    /// <returns>Returns a boolean depending on the operation</returns>
    Task<bool> DeleteOrder(Guid orderID);
}
