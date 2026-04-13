---
name: probuilder-flip-normals
description: |-
  Reverses the normal direction of selected faces, flipping them inside-out.
  Useful for creating interior spaces or fixing inverted faces.
  
  Examples:
  - Flip all faces: leave faceIndices and faceDirection empty
  - Flip top face only: faceDirection=Up
  - Flip specific faces: faceIndices=[0, 2, 4]
---

# Flip face normals in a ProBuilder mesh

## How to Call

```bash
unity-mcp-cli run-tool probuilder-flip-normals --input '{
  "gameObjectRef": "string_value",
  "faceIndices": "string_value",
  "faceDirection": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool probuilder-flip-normals --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool probuilder-flip-normals --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRef` | `any` | Yes | Reference to the GameObject with a ProBuilderMesh component. |
| `faceIndices` | `any` | No | Array of face indices to flip. If empty and faceDirection is empty, flips all faces. |
| `faceDirection` | `any` | No | Semantic face selection by direction. If empty and faceIndices is empty, flips all faces. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef"
    },
    "faceIndices": {
      "$ref": "#/$defs/System.Int32[]"
    },
    "faceDirection": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.FaceDirection"
    }
  },
  "$defs": {
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef": {
      "type": "object",
      "properties": {
        "instanceID": {
          "type": "integer",
          "description": "instanceID of the UnityEngine.Object. If it is '0' and 'path', 'name', 'assetPath' and 'assetGuid' is not provided, empty or null, then it will be used as 'null'. Priority: 1 (Recommended)"
        },
        "path": {
          "type": "string",
          "description": "Path of a GameObject in the hierarchy Sample 'character/hand/finger/particle'. Priority: 2."
        },
        "name": {
          "type": "string",
          "description": "Name of a GameObject in hierarchy. Priority: 3."
        },
        "assetType": {
          "$ref": "#/$defs/System.Type",
          "description": "Type of the asset."
        },
        "assetPath": {
          "type": "string",
          "description": "Path to the asset within the project. Starts with 'Assets/'"
        },
        "assetGuid": {
          "type": "string",
          "description": "Unique identifier for the asset."
        }
      },
      "required": [
        "instanceID"
      ],
      "description": "Find GameObject in opened Prefab or in the active Scene."
    },
    "System.Int32[]": {
      "type": "array",
      "items": {
        "type": "integer"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.FaceDirection": {
      "type": "string",
      "enum": [
        "Up",
        "Down",
        "Left",
        "Right",
        "Forward",
        "Back"
      ]
    }
  },
  "required": [
    "gameObjectRef"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+FlipNormalsResponse"
    }
  },
  "$defs": {
    "System.Int32[]": {
      "type": "array",
      "items": {
        "type": "integer"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+FlipNormalsResponse": {
      "type": "object",
      "properties": {
        "facesFlipped": {
          "type": "integer"
        },
        "selectionMethod": {
          "type": "string"
        },
        "faceIndices": {
          "$ref": "#/$defs/System.Int32[]"
        },
        "totalFaceCount": {
          "type": "integer"
        },
        "totalVertexCount": {
          "type": "integer"
        }
      },
      "required": [
        "facesFlipped",
        "totalFaceCount",
        "totalVertexCount"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

