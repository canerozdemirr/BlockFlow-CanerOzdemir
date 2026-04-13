---
name: animation-create
description: Create Unity's Animation asset files (AnimationClip). Creates folders recursively if they do not exist. Each path should start with 'Assets/' and end with '.anim'.
---

# Animation / Create

## How to Call

```bash
unity-mcp-cli run-tool animation-create --input '{
  "sourcePaths": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool animation-create --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool animation-create --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `sourcePaths` | `any` | Yes | The paths of the animation assets to create. Each path should start with 'Assets/' and end with '.anim'. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "sourcePaths": {
      "$ref": "#/$defs/System.String[]"
    }
  },
  "$defs": {
    "System.String[]": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "required": [
    "sourcePaths"
  ]
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreateAnimationResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreatedAnimationInfo>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreatedAnimationInfo"
      }
    },
    "com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreatedAnimationInfo": {
      "type": "object",
      "properties": {
        "path": {
          "type": "string"
        },
        "instanceId": {
          "type": "integer"
        },
        "name": {
          "type": "string"
        }
      },
      "required": [
        "instanceId"
      ]
    },
    "System.Collections.Generic.List<System.String>": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreateAnimationResponse": {
      "type": "object",
      "properties": {
        "createdAssets": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Animation.AnimationTools+CreatedAnimationInfo>"
        },
        "errors": {
          "$ref": "#/$defs/System.Collections.Generic.List<System.String>"
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

