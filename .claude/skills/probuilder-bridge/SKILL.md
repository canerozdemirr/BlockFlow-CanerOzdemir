---
name: probuilder-bridge
description: |-
  Creates a new face connecting two edges.
  Useful for connecting separate parts of geometry or filling gaps.
  
  Example:
  - edgeA=[0,1], edgeB=[4,5] creates a quad face between the two edges
---

# Bridge two edges in a ProBuilder mesh

## How to Call

```bash
unity-mcp-cli run-tool probuilder-bridge --input '{
  "gameObjectRef": "string_value",
  "edgeA": "string_value",
  "edgeB": "string_value",
  "allowNonManifold": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool probuilder-bridge --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool probuilder-bridge --input-file - <<'EOF'
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
| `edgeA` | `any` | Yes | First edge as [vertexA, vertexB]. |
| `edgeB` | `any` | Yes | Second edge as [vertexA, vertexB]. |
| `allowNonManifold` | `boolean` | No | If true, allows creation of non-manifold geometry (edges shared by more than 2 faces). |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRef": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef"
    },
    "edgeA": {
      "$ref": "#/$defs/System.Int32[]"
    },
    "edgeB": {
      "$ref": "#/$defs/System.Int32[]"
    },
    "allowNonManifold": {
      "type": "boolean"
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
    }
  },
  "required": [
    "gameObjectRef",
    "edgeA",
    "edgeB"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+BridgeResponse"
    }
  },
  "$defs": {
    "System.Int32[]": {
      "type": "array",
      "items": {
        "type": "integer"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+BridgeResponse": {
      "type": "object",
      "properties": {
        "edgeA": {
          "$ref": "#/$defs/System.Int32[]"
        },
        "edgeB": {
          "$ref": "#/$defs/System.Int32[]"
        },
        "newFaceIndex": {
          "type": "integer"
        },
        "allowNonManifold": {
          "type": "boolean"
        },
        "faceCountBefore": {
          "type": "integer"
        },
        "faceCountAfter": {
          "type": "integer"
        },
        "facesAdded": {
          "type": "integer"
        },
        "totalVertexCount": {
          "type": "integer"
        },
        "totalEdgeCount": {
          "type": "integer"
        }
      },
      "required": [
        "newFaceIndex",
        "allowNonManifold",
        "faceCountBefore",
        "faceCountAfter",
        "facesAdded",
        "totalVertexCount",
        "totalEdgeCount"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

