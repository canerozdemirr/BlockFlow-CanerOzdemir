---
name: probuilder-merge-objects
description: |-
  Combines multiple ProBuilder meshes into a single mesh.
  Useful for optimizing draw calls or creating a unified object from parts.
  The first mesh in the list becomes the target that others merge into.
  
  Example: Merge a table made of separate leg and top meshes into one object.
---

# Merge multiple ProBuilder meshes into one

## How to Call

```bash
unity-mcp-cli run-tool probuilder-merge-objects --input '{
  "gameObjectRefs": "string_value",
  "deleteSourceObjects": false
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool probuilder-merge-objects --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool probuilder-merge-objects --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `gameObjectRefs` | `any` | Yes | Array of GameObject references with ProBuilderMesh components to merge. First object becomes the merge target. |
| `deleteSourceObjects` | `boolean` | No | If true, delete the source GameObjects after merging (except the target). Default is true. |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "gameObjectRefs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef[]"
    },
    "deleteSourceObjects": {
      "type": "boolean"
    }
  },
  "$defs": {
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
    "System.Type": {
      "type": "string"
    },
    "com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.GameObjectRef",
        "description": "Find GameObject in opened Prefab or in the active Scene."
      }
    }
  },
  "required": [
    "gameObjectRefs"
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
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+MergeObjectsResponse"
    }
  },
  "$defs": {
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SourceObjectInfo>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SourceObjectInfo"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SourceObjectInfo": {
      "type": "object",
      "properties": {
        "index": {
          "type": "integer"
        },
        "name": {
          "type": "string"
        },
        "status": {
          "type": "string"
        }
      },
      "required": [
        "index"
      ]
    },
    "System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+AdditionalMeshInfo>": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+AdditionalMeshInfo"
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+AdditionalMeshInfo": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string"
        },
        "instanceId": {
          "type": "integer"
        }
      },
      "required": [
        "instanceId"
      ]
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+MergeObjectsResponse": {
      "type": "object",
      "properties": {
        "mergedMeshCount": {
          "type": "integer"
        },
        "resultMeshCount": {
          "type": "integer"
        },
        "targetObjectName": {
          "type": "string"
        },
        "targetInstanceId": {
          "type": "integer"
        },
        "objectsDeleted": {
          "type": "integer"
        },
        "totalFacesBefore": {
          "type": "integer"
        },
        "totalFacesAfter": {
          "type": "integer"
        },
        "totalVerticesBefore": {
          "type": "integer"
        },
        "totalVerticesAfter": {
          "type": "integer"
        },
        "sourceObjects": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+SourceObjectInfo>"
        },
        "additionalMeshes": {
          "$ref": "#/$defs/System.Collections.Generic.List<com.IvanMurzak.Unity.MCP.Editor.API.Tool_ProBuilder+AdditionalMeshInfo>"
        }
      },
      "required": [
        "mergedMeshCount",
        "resultMeshCount",
        "targetInstanceId",
        "objectsDeleted",
        "totalFacesBefore",
        "totalFacesAfter",
        "totalVerticesBefore",
        "totalVerticesAfter"
      ]
    }
  },
  "required": [
    "result"
  ]
}
```

