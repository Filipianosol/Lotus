using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Lotus.API.Odyssey;
using Lotus.Managers.Hotkeys;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.Interfaces;
using VentLib.Utilities.Harmony.Attributes;
using VentLib.Utilities.Optionals;

namespace Lotus.Options;

[Localized("OptionShower")]
public class OptionShower
{
    [QuickPostfix(typeof(GameOptionsManager), nameof(GameOptionsManager.SaveNormalHostOptions))]
    public static void VanillaUpdateShower() => GetOptionShower().Update();
    
    private static Optional<OptionShower> Instance = Optional<OptionShower>.Null();

    
    [Localized("ActiveRolesList")]
    private static string ActiveRolesList = "Active Role List";
    [Localized("NextPage")]
    private static string NextPageString = "Press (Tab) To Advance Page";

    private List<ShowerPage> pages = new();
    private List<string> pageContent = null!;

    private int currentPage;
    private bool updated;

    private OptionShower()
    {
        HotkeyManager.Bind(KeyCode.Tab, KeyCode.RightControl).If(p => p.State(GameState.InLobby)).Do(Previous);
        HotkeyManager.Bind(KeyCode.Tab).If(p => p.State(GameState.InLobby)).Do(Next);
    }

    public static OptionShower GetOptionShower()
    {
        return Instance.OrElseSet(() =>
        {
            var shower = new OptionShower();
            OptionManager.GetAllManagers().ForEach(manager =>
            {
                manager.RegisterEventHandler(ioe =>
                {
                    if (ioe is not IOptionValueEvent) return;
                    shower.Update();
                });
            });
            return shower;
        });
    }

    public void Update()
    {
        updated = false;
        pages.Do(p => p.Updated = false);
    }

    public string GetPage()
    {
        if (!updated)
        {
            pageContent = pages.SelectMany(p => p.GetPages()).ToList();
            updated = true;
        }

        string bottomText = $"\n{NextPageString} ({currentPage + 1}/{pageContent.Count})";
        return pageContent[currentPage] + bottomText;
    }

    public void AddPage(Func<string> contentSupplier)
    {
        updated = false;
        pages.Add(new ShowerPage(contentSupplier));
    }

    public void Next()
    {
        currentPage++;
        if (currentPage >= pageContent.Count) currentPage = 0;
    }

    public void Previous()
    {
        currentPage--;
        if (currentPage < 0) currentPage = pageContent.Count - 1;
    }
}

public class ShowerPage
{
    public static int MaxLines = 30;

    private Func<string> supplier;
    private Optional<List<string>> pages = Optional<List<string>>.Null();
    internal bool Updated;

    public ShowerPage(Func<string> supplier)
    {
        this.supplier = supplier;
    }

    public List<string> GetPages()
    {
        if (!Updated) pages = Optional<List<string>>.Of(UpdatePages());
        return pages.OrElseSet(UpdatePages);
    }

    private List<string> UpdatePages()
    {
        string content = supplier();
        List<string> p = new();
        string[] lines = content.Split("\n");
        if (lines.Length < MaxLines) {
            Updated = true;
            p.Add(content);
            return p;
        }

        List<string> lineBuffer = new();
        int i = 0;
        while (i < lines.Length)
        {
            bool flush = false;
            string line = lines[i];
            if (line == "")
            {
                flush = true;
                for (int j = i + 1; j < lines.Length && (j-i-1) + lineBuffer.Count < MaxLines && flush; j++)
                    if (lines[j] == "") flush = false;
            }
            lineBuffer.Add(lines[i++]);

            if (!flush) continue;
            p.Add(lineBuffer.Join(delimiter: "\n"));
            lineBuffer.Clear();

        }

        Updated = true;

        return p;
    }
}