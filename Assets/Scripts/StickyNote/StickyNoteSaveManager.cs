using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StickyNoteSaveManager : MonoBehaviour
{
    [Header("Sticky Note Prefab")]
    [SerializeField] private GameObject stickyNotePrefab; // Prefab used to instantiate sticky notes when loading

    private string saveFilePath; // File path where sticky notes data is saved

    private void Awake()
    {
        // Define the file path for saving/loading sticky notes in persistent data folder
        saveFilePath = Path.Combine(Application.persistentDataPath, "StickyNotes.json");
    }

    private void Start()
    {
        // Load sticky notes from saved file on game start
        LoadStickyNotes();
    }

    private void OnApplicationQuit()
    {
        // Save sticky notes data before application quits
        SaveStickyNotes();
    }

    /// <summary>
    /// Finds all sticky notes in the scene, serializes their data, and writes to JSON file.
    /// </summary>
    private void SaveStickyNotes()
    {
        // Find all active StickyNote components in the scene
        StickyNote[] stickyNotes = FindObjectsByType<StickyNote>(FindObjectsSortMode.None);

        // Prepare list to store serializable sticky note data
        List<StickyNoteData> dataList = new List<StickyNoteData>();

        // Iterate over each sticky note and extract relevant data
        foreach (var note in stickyNotes)
        {
            dataList.Add(new StickyNoteData
            {
                position = note.transform.position,
                rotation = note.transform.rotation,
                noteText = note.GetNoteText()  // Get the text content of the note
            });
        }

        // Wrap list into a container for JSON serialization
        string json = JsonUtility.ToJson(new StickyNoteSaveDataWrapper { notes = dataList }, true);

        // Write JSON string to persistent file
        File.WriteAllText(saveFilePath, json);
    }

    /// <summary>
    /// Loads sticky notes from JSON file and instantiates them in the scene.
    /// </summary>
    private void LoadStickyNotes()
    {
        // Check if save file exists; if not, log and return
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("[StickyNoteSaveManager] No saved sticky notes found.");
            return;
        }

        // Read JSON content from file
        string json = File.ReadAllText(saveFilePath);

        // Deserialize JSON into wrapper object containing list of notes
        StickyNoteSaveDataWrapper loadedData = JsonUtility.FromJson<StickyNoteSaveDataWrapper>(json);

        // Instantiate sticky note prefab for each saved note and set its data
        foreach (var noteData in loadedData.notes)
        {
            GameObject newNote = Instantiate(stickyNotePrefab, noteData.position, noteData.rotation);

            StickyNote stickyNote = newNote.GetComponent<StickyNote>();
            if (stickyNote != null)
            {
                stickyNote.SetNoteText(noteData.noteText);  // Restore note text content
            }
        }

        Debug.Log($"[StickyNoteSaveManager] Loaded {loadedData.notes.Count} sticky notes.");
    }

    /// <summary>
    /// Wrapper class to hold list of sticky notes for JSON serialization
    /// </summary>
    [System.Serializable]
    private class StickyNoteSaveDataWrapper
    {
        public List<StickyNoteData> notes;
    }

    /// <summary>
    /// Serializable class representing the data needed to save/load a sticky note
    /// </summary>
    [System.Serializable]
    public class StickyNoteData
    {
        public Vector3 position; // Position of the sticky note in world space
        public Quaternion rotation; // Rotation of the sticky note
        public string noteText; // Text content of the sticky note
    }
}
