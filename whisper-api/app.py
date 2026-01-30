from fastapi import FastAPI, UploadFile, File
from faster_whisper import WhisperModel
import tempfile
import os

app = FastAPI(title="Whisper STT API")

# Load model once at startup
model = WhisperModel(
    "small",
    device="cpu",
    compute_type="int8"  # fast & low memory
)

@app.post("/transcribe")
async def transcribe(file: UploadFile = File(...)):
    # Save uploaded file temporarily
    with tempfile.NamedTemporaryFile(delete=False) as tmp:
        tmp.write(await file.read())
        audio_path = tmp.name

    segments, info = model.transcribe(audio_path)

    text = ""
    results = []
    for segment in segments:
        text += segment.text + " "
        results.append({
            "start": segment.start,
            "end": segment.end,
            "text": segment.text
        })

    os.remove(audio_path)

    return {
        "language": info.language,
        "text": text.strip(),
        "segments": results
    }
