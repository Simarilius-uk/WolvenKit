using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Win32;
using WolvenKit.App.Extensions;
using WolvenKit.App.Helpers;
using WolvenKit.App.Interaction;
using WolvenKit.App.Models.ProjectManagement;
using WolvenKit.App.ViewModels.Shell;

namespace WolvenKit.App.ViewModels.HomePage.Pages;

public partial class WelcomePageViewModel : PageViewModel
{
    #region Fields

    private readonly IRecentlyUsedItemsService _recentlyUsedItemsService;

    private readonly AppViewModel _mainViewModel;

    private readonly ReadOnlyObservableCollection<RecentlyUsedItemModel> _recentlyUsedItems;

    #endregion Fields

    #region Constructors

    public WelcomePageViewModel(IRecentlyUsedItemsService recentlyUsedItemsService, AppViewModel mainViewModel)
    {
        _recentlyUsedItemsService = recentlyUsedItemsService;
        _mainViewModel = mainViewModel;

        recentlyUsedItemsService.Items
            .Connect()
            //.ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _recentlyUsedItems)
            .Subscribe(OnRecentlyUsedItemsChanged);
    }

    private void OnRecentlyUsedItemsChanged(IChangeSet<RecentlyUsedItemModel, string> obj)
    {
        ConvertRecentProjects();

        var changeSet = _recentlyUsedItems
            .ToObservableChangeSet();

        changeSet
            .WhenAnyPropertyChanged()
            .Subscribe(_ => ConvertRecentProjects());
    }

    #endregion Constructors

    #region Properties

    public string DiscordLink = "https://discord.gg/tKZXma5SaA";
    public string OpenCollectiveLink = "https://opencollective.com/redmodding";
    public string PatreonLink = "https://www.patreon.com/m/RedModdingTools";
    public string TwitterLink = "https://twitter.com/ModdingRed";
    public string YoutubeLink = "https://www.youtube.com/channel/UCl3JpsP49JgYLMYAYQvoaLg";

    [ObservableProperty]
    private ObservableCollection<FancyProjectObject> _fancyPinnedProjects = new();

    [ObservableProperty]
    private ObservableCollection<FancyProjectObject> _fancyProjects = new();

    [ObservableProperty]
    private List<RecentlyUsedItemModel> _pinnedItems = new();

    [ObservableProperty]
    private bool _showPinned;

    #endregion Properties

    #region commands

    [RelayCommand]
    private void OpenProject(string s)
    {
        _mainViewModel.OpenProjectCommand.Execute(s);
    }

    [RelayCommand]
    private void DeleteProject(string s)
    {
        _mainViewModel.DeleteProjectCommand.Execute(s);
    }

    [RelayCommand]
    private void NewProject()
    {
        _mainViewModel.NewProjectCommand.SafeExecute();
    }

    [RelayCommand]
    private void OpenLink(string link)
    {
        var ps = new ProcessStartInfo(link)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(ps);
    }

    [RelayCommand]
    private async Task OpenInExplorer(string parameter)
    {
        if (!File.Exists(parameter))
        {
            parameter = await LocateMissingProjectAsync(parameter);
        }

        if (string.IsNullOrEmpty(parameter))
        {
            return;
        }

        Process.Start("explorer.exe", $"/select, \"{parameter}\"");
    }

    [RelayCommand]
    private void PinItem(string parameter) => _recentlyUsedItemsService.PinItem(parameter);

    [RelayCommand]
    private void CloseHomePage() => _mainViewModel.CloseModalCommand.Execute(null);

    [RelayCommand]
    private void UnpinItem(string parameter)
    {
        //Argument.IsNotNullOrWhitespace(() => parameter);

        //_recentlyUsedItemsService.UnpinItem(parameter);
    }

    #endregion


    private async Task<string> LocateMissingProjectAsync(string parameter)
    {
        var result = await Interactions.ShowMessageBoxAsync("The file doesn't seem to exist. Would you like to locate it?", "Project not found");
        var delete = false;
        switch (result)
        {
            case WMessageBoxResult.OK:
            case WMessageBoxResult.Yes:
                delete = true;
                break;
            case WMessageBoxResult.None:
            case WMessageBoxResult.Cancel:
            case WMessageBoxResult.No:
            case WMessageBoxResult.Custom:
            default:
                break;
        }
        if (!delete)
        {
            var items = _recentlyUsedItemsService.Items.Items
                .Where(_ => _.Name == parameter)
                .ToList();
            if (items.Count > 0)
            {
                var f = items.FirstOrDefault();
                if (f is not null)
                {
                    _recentlyUsedItemsService.RemoveItem(f);
                }
            }
            return "";
        }
        else
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Locate the WolvenKit project",
                Filter = "Cyberpunk 2077 Project|*.cpmodproj"
            };

            if (dlg.ShowDialog() != true)
            {
                return "";
            }

            var file = dlg.FileName;
            if (string.IsNullOrEmpty(file))
            {
                return "";
            }

            parameter = file;

            var items = _recentlyUsedItemsService.Items.Items
                .Where(_ => Path.GetFileName(_.Name) == Path.GetFileName(parameter))
                .ToList();
            if (items.Count > 0)
            {
                var item = items.First();
                _recentlyUsedItemsService.AddItem(new RecentlyUsedItemModel(parameter, item.DateTime, item.LastOpened));
                _recentlyUsedItemsService.RemoveItem(item);
                return parameter;
            }
        }

        return "";
    }

    private void ConvertRecentProjects() // Converts Recent projects for the homepage.
    {
        DispatcherHelper.RunOnMainThread(() => FancyPinnedProjects.Clear());
        DispatcherHelper.RunOnMainThread(() => FancyProjects.Clear());

        var sorted = _recentlyUsedItems.ToList();
        sorted.Sort(delegate (RecentlyUsedItemModel a, RecentlyUsedItemModel b)
        {
            DateTime ad, bd;
            if (a.LastOpened != default)
            {
                ad = a.LastOpened;
            }
            else
            {
                ad = a.DateTime;
            }

            if (b.LastOpened != default)
            {
                bd = b.LastOpened;
            }
            else
            {
                bd = b.DateTime;
            }

            return bd.CompareTo(ad);
        });

        foreach (var item in sorted)
        {
            var fi = new FileInfo(item.Name);

            var cd = item.LastOpened != default ? item.LastOpened : item.DateTime;
            var path = item.Name;

            var newfo = fi.Name.Split('.');
            var image = fi.Directory + "\\" + newfo[0] + "\\" + "img.png";

            if (Path.GetExtension(item.Name).TrimStart('.') == EProjectType.cpmodproj.ToString())
            {
                if (!File.Exists(image))
                {
                    image = "pack://application:,,,/Resources/Media/Images/Application/V Male Logo Cropped.png";
                }

                var newItem = new FancyProjectObject(item, fi.Name, cd, "Cyberpunk 2077", path, image);

                DispatcherHelper.RunOnMainThread(() =>
                {
                    if (newItem.IsPinned)
                    {
                        FancyPinnedProjects.Add(newItem);
                    }
                    else
                    {
                        FancyProjects.Add(newItem);
                    }
                });
            }
        }

        ShowPinned = FancyPinnedProjects.Count > 0;
    }
}
