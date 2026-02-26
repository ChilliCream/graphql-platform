import json
import sys

# Read JSONL and find the last assistant message with substantial text content
with open(sys.argv[1], 'r') as f:
    last_text = None
    for line in f:
        try:
            obj = json.loads(line)
            if obj.get('type') == 'assistant' and 'message' in obj:
                msg = obj['message']
                if 'content' in msg:
                    for block in msg['content']:
                        if block.get('type') == 'text' and len(block.get('text', '')) > 500:
                            last_text = block['text']
        except:
            pass
    if last_text:
        print(last_text)
    else:
        print("NO REPORT FOUND", file=sys.stderr)
        sys.exit(1)
