---
name: probuilder-set-pivot
description: |-
  Changes the pivot (origin) point of a ProBuilder mesh.
  The mesh geometry is adjusted so the pivot moves without changing the visual position.
  
  Examples:
  - Center the pivot: pivotLocation=Center
  - Set pivot to first vertex: pivotLocation=FirstVertex
  - Set custom pivot: pivotLocation=Custom, customPosition=(0, 0, 0)
---

# Set the pivot point of a ProBuilder mesh

## How to Call

```bash
unity-mcp-cli run-tool probuilder-set-pivot --input '{
  "gameObjectRef": "string_value",
  "pivotLocation": "string_value",
  "customPosition": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool probuilder-set-pivot --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool probuilder-set-pivot --input-file - <<'EOF'
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
| `pivotLocation` | `string` | No | Where to place the pivot. |
| `customPosition` | `any` | No | Custom world position for pivot (only used when pivotLocation=Custom). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef"
    },
    "pivotLocation": {
      "type": "string",
      "enum": [
        "Center",
        "FirstVertex",
        "Custom"
      ]
    },
    "customPosition": {
      "$ref": "#/$defs/UnityEngine.Vector3"
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
    "UnityEngine.Vector3": {
      "type": "object",
      "properties": {
        "x": {
          "type": "number"
        },
        "y": {
          "type": "number"
        },
        "z": {
          "type": "number"
        }
      },
      "required": [
        "x",
        "y",
        "z"
      ],
      "additionalProperties": false
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SetPivotResponse"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SetPivotResponse": {
      "type": "object",
      "properties": {
        "pivotLocation": {
          "type": "string"
        },
        "oldPivot": {
          "type": "string"
        },
        "newPivot": {
          "type": "string"
        },
        "offsetApplied": {
          "type": "string"
        },
        "gameObjectName": {
          "type": "string"
        },
        "newPosition": {
          "type": "string"
        }
      }
    }
  },
  "required": [
    "result"
  ]
}
```

