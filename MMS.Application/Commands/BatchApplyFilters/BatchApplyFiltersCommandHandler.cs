using MMS.Application.Abstract.Command;
using MMS.Core.Filters.Abstract;

namespace MMS.Application.Commands.BatchApplyFilters;

public class BatchApplyFiltersCommandHandler 
    : ICommandHandler<BatchApplyFiltersCommand, BatchApplyFiltersCommandResult>
{
    private readonly IImageFilterFactory _filterFactory;

    public BatchApplyFiltersCommandHandler(IImageFilterFactory filterFactory)
    {
        _filterFactory = filterFactory;
    }

    public Task<BatchApplyFiltersCommandResult> HandleAsync(BatchApplyFiltersCommand command, CancellationToken cancellationToken)
    {
        foreach (var filterType in command.Filters)
        {
            var filter = _filterFactory.Create(filterType);
            filter.Execute(command.Bitmap);
        }

        return Task.FromResult(new BatchApplyFiltersCommandResult(command.Bitmap));
    }
}