﻿using Fluxor;

using SciMaterials.Contracts.API.DTO.Files;
using SciMaterials.Contracts.WebApi.Clients.Files;
using SciMaterials.UI.BWASM.Models;
using SciMaterials.UI.BWASM.States.FileUpload;

namespace SciMaterials.UI.BWASM.Services;

public class FileUploadScheduleService : IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FileUploadScheduleService> _logger;

    private readonly Queue<FileUploadData> _uploadRequests = new();
    private readonly PeriodicTimer _sender; 


    public FileUploadScheduleService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FileUploadScheduleService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;

        _sender = new(TimeSpan.FromSeconds(30));

        _ = Upload();
    }

    public void ScheduleUpload(FileUploadData data)
    {
        _logger.LogInformation("Schedule upload of file {name}", data.FileName);
        _uploadRequests.Enqueue(data);
    }

    private async Task Upload()
    {
        while (true)
        {
            if(!await _sender.WaitForNextTickAsync()) return;
            if(_uploadRequests.Count <= 0) continue;

            if(!_uploadRequests.TryDequeue(out FileUploadData? data)) continue;

            using var scope = _serviceScopeFactory.CreateScope();

            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            var filesClient = scope.ServiceProvider.GetRequiredService<IFilesClient>();

            _logger.LogInformation("Building upload data for file {name}", data.FileName);
            dispatcher.Dispatch(new FileUploading(data.Id));
            
            var result = await filesClient.UploadAsync(
                data.File.OpenReadStream(),
                new UploadFileRequest()
                {
                    Name = data.FileName,
                    Size = data.File.Size,
                    // TODO
                    Categories = string.Empty,
                    AuthorId = Guid.Empty,
                    ContentTypeName = string.Empty,
                    Title = string.Empty
                },
                data.CancellationToken);

            if (data.CancellationToken.IsCancellationRequested)
            {
                dispatcher.Dispatch(new FileUploadCanceled(data.Id));
                continue;
            }

            if (result.Succeeded)
            {
                dispatcher.Dispatch(new FileUploaded(data.Id));
                continue;
            }

            dispatcher.Dispatch(new FileUploadFailed(data.Id, result.Code));
        }
    }

    public void Dispose()
    {
        _sender.Dispose();
    }
}