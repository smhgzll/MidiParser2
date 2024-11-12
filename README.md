# Simple MIDI File Parser and Synthesizer  

This project provides a basic MIDI file parser and synthesizer, created with the help of ChatGPT. It is designed as an educational tool to help users understand the structure of MIDI files and the fundamentals of sound synthesis.  

## Key Features  
- **MIDI File Parsing**:  
  - Extracts MIDI events with the following properties:  
    - **Timestamp**: Timing of the event.  
    - **Note**: The musical note being played.  
    - **Velocity**: Intensity or volume of the note.  
    - **Channel & Track**: Logical grouping of events.  
    - **Event Type**: Type of MIDI event (e.g., Note On, Note Off).  

- **Synthesizer**:  
  - Built using `OpenTK.Audio.OpenAL`.  
  - Supports **attack** and **release** for smooth and natural note transitions.  

## Purpose  
This project is **not a complete solution** but rather a lightweight tool for:  
- Learning the structure and events in MIDI files.  
- Exploring how MIDI data can be used to generate audio.  

Feel free to expand and customize this project to suit your needs!  
