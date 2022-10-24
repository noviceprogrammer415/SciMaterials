using Microsoft.Extensions.Logging;
using SciMaterials.Contracts.Result;
using SciMaterials.Contracts.WebApi.Clients;

using System.Net.Http.Json;

namespace SciMaterials.WebApi.Clients;

public abstract class ApiClientBase<TId, TResult, TEditRequest> : 
    IApiReadonlyClient<TId, TResult>,
    IApiEditClient<TId, TEditRequest>,
    IApiDeleteClient<TId>
{
    protected readonly ILogger<ApiClientBase<TId, TResult, TEditRequest>> _logger;
    protected readonly HttpClient _httpClient;
    protected string _webApiRoute = string.Empty;

    public ApiClientBase(HttpClient httpClient, ILogger<ApiClientBase<TId, TResult, TEditRequest>> logger) 
        => (_httpClient, _logger) = (httpClient, logger);

    public virtual async Task<Result<TId>> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Delete entity with id {id}", id);

        var response = await _httpClient.DeleteAsync($"{_webApiRoute}/{id}", cancellationToken);
        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<Result<TId>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("No response received from the server.");
        return result;
    }

    public virtual async Task<Result<TId>> EditAsync(TEditRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Edit entity {request}.", request);

        var response = await _httpClient.PutAsJsonAsync($"{_webApiRoute}/Edit", request, cancellationToken);
        var result = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<Result<TId>>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("No response received from the server.");
        return result;
    }

    public virtual async Task<Result<IEnumerable<TResult>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get all entities");

        var result = await _httpClient.GetFromJsonAsync<Result<IEnumerable<TResult>>>(_webApiRoute, cancellationToken)
            ?? throw new InvalidOperationException("No response received from the server.");
        return result;
    }

    public virtual async Task<Result<TResult>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get entity by id {id}", id);

        var result = await _httpClient.GetFromJsonAsync<Result<TResult>>($"{_webApiRoute}/{id}", cancellationToken)
            ?? throw new InvalidOperationException("No response received from the server.");
        return result;
    }

    public virtual async Task<PageResult<TResult>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get paged entities >>> Page number:{pageNumber}; Page dize {pageSize}", pageNumber, pageSize);

        var result = await _httpClient.GetFromJsonAsync<PageResult<TResult>>($"{_webApiRoute}/page/{pageNumber}/{pageSize}", cancellationToken)
            ?? throw new InvalidOperationException("No response received from the server.");
        return result;
    }
}
