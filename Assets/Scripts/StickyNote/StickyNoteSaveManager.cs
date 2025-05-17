using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StickyNoteSaveManager : MonoBehaviour
{
    [Header("Sticky Note Prefab")]
    [SerializeField] private GameObject stickyNotePrefab;

    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "StickyNotes.json");
    }

    private void Start()
    {
        LoadStickyNotes();
    }

    private void OnApplicationQuit()
    {
        SaveStickyNotes();
    }

    private void SaveStickyNotes()
    {
        StickyNote[] stickyNotes = FindObjectsByType<StickyNote>(FindObjectsSortMode.None);

        List<StickyNoteData> dataList = new List<StickyNoteData>();

        foreach (var note in stickyNotes)
        {
            dataList.Add(new StickyNoteData
            {
                position = note.transform.position,
                rotation = note.transform.rotation,
                noteText = note.GetNoteText()
            });
        }

        string json = JsonUtility.ToJson(new StickyNoteSaveDataWrapper { notes = dataList }, true);
        File.WriteAllText(saveFilePath, json);

        Debug.Log($"[StickyNoteSaveManager] Saved {dataList.Count} sticky notes.");
    }

    private void LoadStickyNotes()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("[StickyNoteSaveManager] No saved sticky notes found.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        StickyNoteSaveDataWrapper loadedData = JsonUtility.FromJson<StickyNoteSaveDataWrapper>(json);

        foreach (var noteData in loadedData.notes)
        {
            GameObject newNote = Instantiate(stickyNotePrefab, noteData.position, noteData.rotation);

            StickyNote stickyNote = newNote.GetComponent<StickyNote>();
            if (stickyNote != null)
            {
                stickyNote.SetNoteText(noteData.noteText);
            }
        }

        Debug.Log($"[StickyNoteSaveManager] Loaded {loadedData.notes.Count} sticky notes.");
    }

    [System.Serializable]
    private class StickyNoteSaveDataWrapper
    {
        public List<StickyNoteData> notes;
    }

    [System.Serializable]
    public class StickyNoteData
    {
        public Vector3 position;
        public Quaternion rotation;
        public string noteText;
    }
}
