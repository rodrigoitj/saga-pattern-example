namespace Saga.Orchestrator.Infrastructure.HttpClients;

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Saga.Orchestrator.Application.Interfaces;
using Saga.Orchestrator.Domain.Models;

/// <summary>
/// HTTP Client implementation for calling external booking services.
/// </summary>
public class BookingServiceHttpClient : IBookingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingServiceHttpClient> _logger;
    private readonly string _flightServiceUrl;
    private readonly string _hotelServiceUrl;
    private readonly string _carServiceUrl;

    public BookingServiceHttpClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BookingServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _flightServiceUrl = configuration["Services:FlightServiceUrl"] ?? "http://localhost:5002";
        _hotelServiceUrl = configuration["Services:HotelServiceUrl"] ?? "http://localhost:5003";
        _carServiceUrl = configuration["Services:CarServiceUrl"] ?? "http://localhost:5004";
    }

    public async Task<FlightBookingResult> BookFlightAsync(
        FlightBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_flightServiceUrl}/api/flights",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FlightBookingResult>(responseContent);
                return result ?? new FlightBookingResult { IsSuccess = true };
            }
            else
            {
                return new FlightBookingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Flight service returned {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Flight service");
            return new FlightBookingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<HotelBookingResult> BookHotelAsync(
        HotelBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_hotelServiceUrl}/api/hotels",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<HotelBookingResult>(responseContent);
                return result ?? new HotelBookingResult { IsSuccess = true };
            }
            else
            {
                return new HotelBookingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Hotel service returned {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Hotel service");
            return new HotelBookingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CarBookingResult> BookCarAsync(
        CarBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_carServiceUrl}/api/cars",
                content,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<CarBookingResult>(responseContent);
                return result ?? new CarBookingResult { IsSuccess = true };
            }
            else
            {
                return new CarBookingResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Car service returned {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Car service");
            return new CarBookingResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> CancelFlightAsync(Guid flightBookingId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"{_flightServiceUrl}/api/flights/{flightBookingId}/cancel",
                null,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling flight booking");
            return false;
        }
    }

    public async Task<bool> CancelHotelAsync(Guid hotelBookingId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"{_hotelServiceUrl}/api/hotels/{hotelBookingId}/cancel",
                null,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling hotel booking");
            return false;
        }
    }

    public async Task<bool> CancelCarAsync(Guid carBookingId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"{_carServiceUrl}/api/cars/{carBookingId}/cancel",
                null,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling car booking");
            return false;
        }
    }
}
