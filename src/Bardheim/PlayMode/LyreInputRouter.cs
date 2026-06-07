using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Managers;
using Bardheim.Audio;
using Bardheim.Instruments;
using Bardheim.Network;
using Bardheim.Notes;
using Bardheim.UI;
using UnityEngine;

namespace Bardheim.PlayMode;

public sealed class LyreInputRouter
{
    private const string CycleButtonName = "Bardheim_CycleNoteSet";
    private const string HotbarButtonPrefix = "Hotbar";

    private static readonly KeyCode[] NoteKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8
    };

    private readonly NoteSetSelector _noteSetSelector;
    private readonly LyrePlayMode _playMode;
    private readonly LocalNotePlayback _playback;
    private readonly LocalDrumPlayback _drumPlayback;
    private readonly LocalFlutePlayback _flutePlayback;
    private readonly ILyreNetworkNoteEmitter _noteEmitter;
    private readonly PlayerFeedback _feedback;
    private readonly ManualLogSource _logger;
    private bool _registered;

    public LyreInputRouter(
        NoteSetSelector noteSetSelector,
        LyrePlayMode playMode,
        LocalNotePlayback playback,
        LocalDrumPlayback drumPlayback,
        LocalFlutePlayback flutePlayback,
        ILyreNetworkNoteEmitter noteEmitter,
        PlayerFeedback feedback,
        ManualLogSource logger)
    {
        _noteSetSelector = noteSetSelector;
        _playMode = playMode;
        _playback = playback;
        _drumPlayback = drumPlayback;
        _flutePlayback = flutePlayback;
        _noteEmitter = noteEmitter;
        _feedback = feedback;
        _logger = logger;
    }

    public void RegisterButtons()
    {
        if (_registered)
        {
            return;
        }

        for (var index = 0; index < NoteKeys.Length; index++)
        {
            var slot = index + 1;
            InputManager.Instance.AddButton(
                Plugin.PluginGuid,
                new ButtonConfig
                {
                    Name = GetButtonName(slot),
                    Key = NoteKeys[index],
                    Hint = $"Lyre note {slot}",
                    BlockOtherInputs = true,
                    ActiveInGUI = false
                });
        }

        InputManager.Instance.AddButton(
            Plugin.PluginGuid,
            new ButtonConfig
            {
                Name = CycleButtonName,
                Key = KeyCode.Alpha9,
                Hint = "Cycle lyre tuning",
                BlockOtherInputs = true,
                ActiveInGUI = false
            });

        _registered = true;
    }

    public void Update(Player player, string? activeInstrumentId, int maxSimultaneousNotes, float volume)
    {
        if (!_playMode.IsActive || player is null)
        {
            return;
        }

        if (activeInstrumentId == InstrumentCatalog.LyreId && TryCycleNoteSet())
        {
            return;
        }

        for (var slot = 1; slot <= NoteKeys.Length; slot++)
        {
            var notePressed =
                ZInput.GetButtonDown(GetButtonName(slot)) ||
                ZInput.GetButtonDown(GetHotbarButtonName(slot)) ||
                ZInput.GetKeyDown(NoteKeys[slot - 1], false);

            ZInput.ResetButtonStatus(GetHotbarButtonName(slot));

            if (!notePressed)
            {
                continue;
            }

            if (activeInstrumentId == InstrumentCatalog.LyreId)
            {
                PlayLyreSlot(player, slot, maxSimultaneousNotes, volume);
            }
            else if (activeInstrumentId == InstrumentCatalog.DrumsId)
            {
                PlayDrumSlot(player, slot, maxSimultaneousNotes, volume);
            }
            else if (activeInstrumentId == InstrumentCatalog.FluteId)
            {
                PlayFluteSlot(player, slot, maxSimultaneousNotes, volume);
            }
        }
    }

    private void PlayLyreSlot(Player player, int slot, int maxSimultaneousNotes, float volume)
    {
        if (!_noteSetSelector.Current.Map.TryGet(slot, out var note))
        {
            return;
        }

        var request = new NoteRequest(note, player.transform.position);
        if (_playback.Play(request, maxSimultaneousNotes, volume))
        {
            _noteEmitter.Emit(request, LyreNetworkNoteSource.Manual, volume);
            _logger.LogInfo($"Played lyre note {note.Name} from slot {slot}.");
        }
    }

    private void PlayDrumSlot(Player player, int slot, int maxSimultaneousHits, float volume)
    {
        var hitId = slot switch
        {
            1 => "kick",
            2 => "snare",
            3 => "rim",
            4 => "clap",
            5 => "muted",
            6 => "open",
            7 => "low_tom",
            8 => "high_tom",
            _ => null
        };

        if (hitId is null)
        {
            return;
        }

        if (_drumPlayback.Play(hitId, player.transform.position, maxSimultaneousHits, volume))
        {
            _noteEmitter.Emit(InstrumentCatalog.DrumsId, hitId, player.transform.position, LyreNetworkNoteSource.Manual, volume);
            _logger.LogInfo($"Played drum hit {hitId} from slot {slot}.");
        }
    }

    private void PlayFluteSlot(Player player, int slot, int maxSimultaneousNotes, float volume)
    {
        var flute = InstrumentCatalog.CreateDefaultInstruments()[2];
        var noteSet = flute.NoteSets[0];
        if (!noteSet.Map.TryGet(slot, out var note))
        {
            return;
        }

        var request = new NoteRequest(note, player.transform.position);
        if (_flutePlayback.Play(request, maxSimultaneousNotes, volume))
        {
            _noteEmitter.Emit(InstrumentCatalog.FluteId, note.Name, player.transform.position, LyreNetworkNoteSource.Manual, volume);
            _logger.LogInfo($"Played flute note {note.Name} from slot {slot}.");
        }
    }

    private bool TryCycleNoteSet()
    {
        var cyclePressed =
            ZInput.GetButtonDown(CycleButtonName) ||
            ZInput.GetButtonDown(GetHotbarButtonName(9)) ||
            ZInput.GetKeyDown(KeyCode.Alpha9, false);

        ZInput.ResetButtonStatus(GetHotbarButtonName(9));

        if (!cyclePressed)
        {
            return false;
        }

        var noteSet = _noteSetSelector.CycleNext();
        _feedback.Center($"Lyre tuning: {noteSet.Name}");
        return true;
    }

    private static string GetButtonName(int slot)
    {
        return $"Bardheim_Note{slot}";
    }

    private static string GetHotbarButtonName(int slot)
    {
        return $"{HotbarButtonPrefix}{slot}";
    }
}
