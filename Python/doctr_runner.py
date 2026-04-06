import json
import sys
from pathlib import Path

from doctr.io import DocumentFile
from doctr.models import ocr_predictor


def main():
    if len(sys.argv) < 3:
        raise ValueError("Usage: python doctr_runner.py <input_image> <output_json>")

    input_image = sys.argv[1]
    output_json = sys.argv[2]

    doc = DocumentFile.from_images(input_image)
    model = ocr_predictor(pretrained=True)
    result = model(doc)

    exported = result.export()

    words = []
    full_text_parts = []
    confidences = []

    for page in exported.get("pages", []):
        for block in page.get("blocks", []):
            for line in block.get("lines", []):
                line_words = []
                for word in line.get("words", []):
                    value = word.get("value", "")
                    confidence = float(word.get("confidence", 0.0))
                    geometry = word.get("geometry", ((0, 0), (0, 0)))

                    (x_min, y_min), (x_max, y_max) = geometry

                    words.append({
                        "text": value,
                        "confidence": confidence,
                        "xMin": x_min,
                        "yMin": y_min,
                        "xMax": x_max,
                        "yMax": y_max
                    })

                    line_words.append(value)
                    confidences.append(confidence)

                if line_words:
                    full_text_parts.append(" ".join(line_words))

    average_confidence = sum(confidences) / len(confidences) if confidences else 0.0

    payload = {
        "engine": "docTR",
        "imagePath": input_image,
        "fullText": "\n".join(full_text_parts),
        "confidence": average_confidence,
        "jsonPath": str(Path(output_json).resolve()),
        "words": words
    }

    with open(output_json, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()