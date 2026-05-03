# Unity MCP Inventory

Generated: 2026-05-03

## Endpoint

- Server config name: `unityMCP`
- URL: `http://127.0.0.1:8080/mcp`
- Server: `mcp-for-unity-server`
- Server version: `3.2.4`
- Unity project: `OKEI`
- Unity version: `6000.3.10f1`
- Project root: `D:/GitRepos/OKEI`
- Active scene: `Assets/Scenes/Level4.unity`
- Ready for tools: `true`

## Native Codex Client Status

The Unity MCP HTTP endpoint responds correctly when called directly with MCP
headers. The current Codex MCP client handshake fails with:

```text
Unexpected content type: Some("missing-content-type; body: "), when send initialize request
```

Until that client-level handshake is fixed or the session is restarted with a
compatible transport/config, Unity MCP can still be used through direct HTTP MCP
calls to the endpoint above.

## Resources

| Name | URI |
| --- | --- |
| cameras | `mcpforunity://scene/cameras` |
| custom_tools | `mcpforunity://custom-tools` |
| editor_active_tool | `mcpforunity://editor/active-tool` |
| editor_prefab_stage | `mcpforunity://editor/prefab-stage` |
| editor_selection | `mcpforunity://editor/selection` |
| editor_state | `mcpforunity://editor/state` |
| editor_windows | `mcpforunity://editor/windows` |
| gameobject_api | `mcpforunity://scene/gameobject-api` |
| get_tests | `mcpforunity://tests` |
| menu_items | `mcpforunity://menu-items` |
| prefab_api | `mcpforunity://prefab-api` |
| project_info | `mcpforunity://project/info` |
| project_layers | `mcpforunity://project/layers` |
| project_tags | `mcpforunity://project/tags` |
| renderer_features | `mcpforunity://pipeline/renderer-features` |
| rendering_stats | `mcpforunity://rendering/stats` |
| tool_groups | `mcpforunity://tool-groups` |
| unity_instances | `mcpforunity://instances` |
| volumes | `mcpforunity://scene/volumes` |

## Tools

| Tool | Notes |
| --- | --- |
| apply_text_edits | Core script/text edit helper |
| batch_execute | Batch multiple MCP tool calls |
| create_script | Create Unity scripts |
| debug_request_context | Inspect MCP request context |
| delete_script | Delete Unity scripts |
| execute_code | Execute in-memory C# in Unity Editor |
| execute_custom_tool | Run project-scoped custom tools |
| execute_menu_item | Execute Unity menu items |
| find_gameobjects | Search scene GameObjects |
| find_in_file | Regex search in files |
| get_sha | Get script/file SHA |
| get_test_job | Poll Unity test job |
| manage_animation | Animator/controllers/clips |
| manage_asset | Asset CRUD/search/info |
| manage_build | Build settings and player builds |
| manage_camera | Cameras, Cinemachine, screenshots |
| manage_components | Add/remove/set component properties |
| manage_editor | Editor and play mode controls |
| manage_gameobject | Create/modify/delete GameObjects |
| manage_graphics | Rendering, volumes, URP/HDRP features |
| manage_material | Materials |
| manage_packages | Unity packages |
| manage_physics | Physics helpers |
| manage_prefabs | Prefab operations |
| manage_probuilder | ProBuilder modeling |
| manage_profiler | Profiler helpers |
| manage_scene | Scene operations |
| manage_script | Script management |
| manage_script_capabilities | Script capability info |
| manage_scriptable_object | ScriptableObject management |
| manage_shader | Shader operations |
| manage_texture | Texture operations |
| manage_tools | Activate/deactivate tool groups |
| manage_ui | UI Toolkit helpers |
| manage_vfx | VFX Graph/helpers |
| read_console | Read Unity console |
| refresh_unity | Refresh Unity assets |
| run_tests | Start Unity test runs |
| script_apply_edits | Structured C# edits |
| set_active_instance | Select active Unity instance |
| unity_docs | Official Unity docs lookup |
| unity_reflect | Live C# API reflection |
| validate_script | Validate Unity scripts |

## Tool Groups

| Group | Default | Tools |
| --- | --- | --- |
| core | yes | `batch_execute`, `execute_menu_item`, `find_gameobjects`, `find_in_file`, `manage_asset`, `manage_build`, `manage_camera`, `manage_components`, `manage_editor`, `manage_gameobject`, `manage_graphics`, `manage_material`, `manage_packages`, `manage_physics`, `manage_prefabs`, `manage_scene`, `refresh_unity`, `apply_text_edits`, `create_script`, `delete_script`, `validate_script`, `manage_script`, `get_sha`, `read_console`, `script_apply_edits` |
| animation | no | `manage_animation` |
| docs | no | `unity_docs`, `unity_reflect` |
| probuilder | no | `manage_probuilder` |
| profiling | no | `manage_profiler` |
| scripting_ext | no | `execute_code`, `manage_scriptable_object` |
| testing | no | `run_tests`, `get_test_job` |
| ui | no | `manage_ui` |
| vfx | no | `manage_shader`, `manage_texture`, `manage_vfx` |

