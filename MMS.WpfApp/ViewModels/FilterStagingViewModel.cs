using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MMS.Core.Filters;

namespace MMS.WpfApp.ViewModels;

public sealed class FilterStagingViewModel : INotifyPropertyChanged
{
    public ObservableCollection<FilterViewModel> Filters { get; } = [];
    public Array AvailableFilterTypes => Enum.GetValues<ImageFilterType>();

    public MmsCommand AddFilterCommand { get; }
    public MmsCommand RemoveFilterCommand { get; }
    public MmsCommand MoveFilterUpCommand { get; }
    public MmsCommand MoveFilterDownCommand { get; }

    public MmsCommand? ApplyCommand
    {
        get;
        set
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    public FilterStagingViewModel()
    {
        AddFilterCommand = new MmsCommand(_ => AddFilter());
        RemoveFilterCommand = new MmsCommand(p => RemoveFilter(p as FilterViewModel));
        MoveFilterUpCommand = new MmsCommand(p => MoveFilterUp(p as FilterViewModel), p => CanMoveUp(p as FilterViewModel));
        MoveFilterDownCommand = new MmsCommand(p => MoveFilterDown(p as FilterViewModel), p => CanMoveDown(p as FilterViewModel));
    }

    public void SetApplyCommand(MmsCommand applyCommand)
    {
        ApplyCommand = applyCommand;
        RefreshCommandStates();
    }

    public void ClearFilters()
    {
        if (Filters.Count == 0)
        {
            return;
        }

        foreach (var filter in Filters)
        {
            filter.PropertyChanged -= OnFilterPropertyChanged;
        }

        Filters.Clear();
        RefreshCommandStates();
    }

    private void AddFilter()
    {
        var filter = new FilterViewModel();
        filter.PropertyChanged += OnFilterPropertyChanged;
        Filters.Add(filter);
        RefreshCommandStates();
    }

    private void RemoveFilter(FilterViewModel? filter)
    {
        if (filter != null)
        {
            filter.PropertyChanged -= OnFilterPropertyChanged;
            Filters.Remove(filter);
            RefreshCommandStates();
        }
    }

    private bool CanMoveUp(FilterViewModel? filter)
    {
        return filter != null && Filters.IndexOf(filter) > 0;
    }

    private void MoveFilterUp(FilterViewModel? filter)
    {
        if (filter == null)
        {
            return;
        }

        var index = Filters.IndexOf(filter);

        if (index > 0)
        {
            Filters.Move(index, index - 1);
            RefreshCommandStates();
        }
    }

    private bool CanMoveDown(FilterViewModel? filter)
    {
        return filter != null && Filters.IndexOf(filter) < Filters.Count - 1;
    }

    private void MoveFilterDown(FilterViewModel? filter)
    {
        if (filter == null)
        {
            return;
        }

        var index = Filters.IndexOf(filter);

        if (index < Filters.Count - 1)
        {
            Filters.Move(index, index + 1);
            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        AddFilterCommand.RaiseCanExecuteChanged();
        RemoveFilterCommand.RaiseCanExecuteChanged();
        MoveFilterUpCommand.RaiseCanExecuteChanged();
        MoveFilterDownCommand.RaiseCanExecuteChanged();
        ApplyCommand?.RaiseCanExecuteChanged();
    }

    private void OnFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FilterViewModel.SelectedType) or nameof(FilterViewModel.Options))
        {
            RefreshCommandStates();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
