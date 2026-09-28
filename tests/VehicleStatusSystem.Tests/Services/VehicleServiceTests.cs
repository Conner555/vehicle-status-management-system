using Microsoft.Extensions.Logging;
using Moq;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;
using VehicleStatusSystem.Services;

using VehicleStatusSystem.DTOs;

namespace VehicleStatusSystem.Tests.Services;

public class VehicleServiceTests
{
    [Fact]
    public async Task DeleteAsync_WhenVehicleHasStatusRecords_ThrowsBusinessRuleException()
    {
        // Arrange
        var vehicleRepositoryMock =
            new Mock<IVehicleRepository>();

        var statusRepositoryMock =
            new Mock<IVehicleStatusRepository>();

        var loggerMock =
            new Mock<ILogger<VehicleService>>();

        var vehicle = new Vehicle
        {
            Id = 2,
            PlateNumber = "辽B12345",
            Model = "Test Vehicle",
            Status = "Active"
        };

        vehicleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(2))
            .ReturnsAsync(vehicle);

        statusRepositoryMock
            .Setup(repo => repo.HasRecordsAsync(2))
            .ReturnsAsync(true);

        var service = new VehicleService(
            vehicleRepositoryMock.Object,
            statusRepositoryMock.Object,
            loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.DeleteAsync(2));

        vehicleRepositoryMock.Verify(
            repo => repo.DeleteAsync(It.IsAny<int>()),
            Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_WhenVehicleDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var vehicleRepositoryMock =
            new Mock<IVehicleRepository>();

        var statusRepositoryMock =
            new Mock<IVehicleStatusRepository>();

        var loggerMock =
            new Mock<ILogger<VehicleService>>();

        vehicleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Vehicle?)null);

        var service = new VehicleService(
            vehicleRepositoryMock.Object,
            statusRepositoryMock.Object,
            loggerMock.Object);

        // Act
        bool result =
            await service.DeleteAsync(999);

        // Assert
        Assert.False(result);

        statusRepositoryMock.Verify(
            repo => repo.HasRecordsAsync(It.IsAny<int>()),
            Times.Never);

        vehicleRepositoryMock.Verify(
            repo => repo.DeleteAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenVehicleHasNoStatusRecords_ReturnsTrue()
    {
        // Arrange
        var vehicleRepositoryMock =
            new Mock<IVehicleRepository>();

        var statusRepositoryMock =
            new Mock<IVehicleStatusRepository>();

        var loggerMock =
            new Mock<ILogger<VehicleService>>();

        var vehicle = new Vehicle
        {
            Id = 4,
            PlateNumber = "辽B88888",
            Model = "Delete Test Car",
            Status = "Inactive"
        };

        vehicleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(4))
            .ReturnsAsync(vehicle);

        statusRepositoryMock
            .Setup(repo => repo.HasRecordsAsync(4))
            .ReturnsAsync(false);

        vehicleRepositoryMock
            .Setup(repo => repo.DeleteAsync(4))
            .ReturnsAsync(true);

        var service = new VehicleService(
            vehicleRepositoryMock.Object,
            statusRepositoryMock.Object,
            loggerMock.Object);

        // Act
        bool result =
            await service.DeleteAsync(4);

        // Assert
        Assert.True(result);

        vehicleRepositoryMock.Verify(
            repo => repo.DeleteAsync(4),
            Times.Once);
    }


    [Fact]
    public async Task UpdateAsync_WhenVehicleDoesNotExist_ReturnsNull()
    {
        // Arrange
        var vehicleRepositoryMock =
            new Mock<IVehicleRepository>();

        var statusRepositoryMock =
            new Mock<IVehicleStatusRepository>();

        var loggerMock =
            new Mock<ILogger<VehicleService>>();

        var dto = new UpdateVehicleDto
        {
            PlateNumber = "辽B65675",
            Model = "B Y D",
            Status = "Active"
        };

        vehicleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(999))
            .ReturnsAsync((Vehicle?)null);

        var service = new VehicleService(
            vehicleRepositoryMock.Object,
            statusRepositoryMock.Object,
            loggerMock.Object);

        // Act
        VehicleDto? result =
            await service.UpdateAsync(999, dto);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenPlateNumberAlreadyExists_ThrowsBusinessRuleException()
    {
        // Arrange
        var vehicleRepositoryMock =
            new Mock<IVehicleRepository>();

        var statusRepositoryMock =
            new Mock<IVehicleStatusRepository>();

        var loggerMock =
            new Mock<ILogger<VehicleService>>();


        var vehicle = new Vehicle
        {
            Id = 2,
            PlateNumber = "辽B11111",
            Model = "Test Vehicle",
            Status = "Active"
        };

        var dto = new UpdateVehicleDto
        {
            PlateNumber = "辽B66666",
            Model = "Test Vehicle",
            Status = "Active"
        };

        vehicleRepositoryMock
            .Setup(repo => repo.GetByIdAsync(2))
            .ReturnsAsync(vehicle);

        vehicleRepositoryMock
            .Setup(repo => repo.PlateNumberExistsAsync(dto.PlateNumber, 2))
            .ReturnsAsync(true);

        var service = new VehicleService(
            vehicleRepositoryMock.Object,
            statusRepositoryMock.Object,
            loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UpdateAsync(2, dto));

        vehicleRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.IsAny<Vehicle>()),
            Times.Never);

    }

}