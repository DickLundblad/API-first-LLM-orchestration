# ✅ KLART: Python-tester mappade!

## Vad jag gjorde

Jag skannade ditt Python test-repo i `C:\git\InternalAI\backend\tests` och skapade TestMappings.json.

## 📁 Skapad fil

**`src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json`**

Innehåller 17 test-mappningar från:
- `tests/test_team_api.py` (10 tester)
- `tests/test_auth.py` (2 tester)
- `tests/test_courses.py` (5 tester)

## 📊 Mappade tester

### Team API Tests (10)
- `test_unauthenticated_access_denied` → GetTeamMembers
- `test_admin_sees_all_members` → Login, GetTeamMembers
- `test_ceo_sees_all_members` → Login, GetTeamMembers
- `test_manager_sees_all_members` → Login, GetTeamMembers
- `test_consultant_sees_all_members` → Login, GetTeamMembers
- `test_admin_sees_inactive_members` → Login, GetTeamMembers
- `test_non_admin_filters_out_inactive_members` → Login, GetTeamMembers
- `test_pagination_metadata_first_page` → Login, GetTeamMembers
- `test_pagination_last_page_has_no_more` → Login, GetTeamMembers
- `test_include_inactive_changes_totals` → Login, GetTeamMembers

### Auth Tests (2)
- `test_login_success` → Login
- `test_login_failure` → Login

### Course Tests (5)
- `test_get_courses` → Login, GetCourses
- `test_create_course` → Login, CreateCourse
- `test_update_course` → Login, UpdateCourse
- `test_approve_course` → Login, ApproveCourse
- `test_archive_course` → Login, ArchiveCourse

## 🚀 Testa nu

```powershell
.\Start-McpServer.ps1
```

**Förväntat output:**
```
[CapabilityRegistry] Generated X capabilities from Swagger
[TestMapping] Loaded test mappings from TestMappings.json
[TestMapping] Linked 17 tests to Y capabilities
```

## 📈 Använd från LLM

Via LLM-klient:
```
"Visa test coverage"
→ capability_coverage verktyget

"Visa capability login"
→ Ska visa alla login-tester

"Visa capability getteammembers"
→ Ska visa alla team API tester
```

## ➕ Lägg till fler tester

Du har många fler testfiler:
- test_admin_rights.py
- test_bulk_update.py
- test_check_external.py
- test_csp.py
- test_email_service.py
- test_email_verification.py
- test_field_permissions.py
- test_gdpr_offboarding.py
- test_images_fallback.py
- test_image_upload.py
- test_manager_deactivation.py
- test_migrations.py
- test_models.py
- test_my_team_n_plus_one.py
- test_password_reset.py
- test_permissions.py
- test_security_api.py
- test_team_member_update.py
- (och fler...)

För att lägga till:
1. Redigera TestMappings.json:
```powershell
notepad src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json
```

2. Lägg till enligt samma mönster:
```json
{
  "testId": "tests/test_XXX.py::test_YYY",
  "testName": "Beskrivning",
  "operations": ["Operation1", "Operation2"],
  "capabilities": ["operation1", "operation2"]
}
```

3. Starta om servern

## 💡 Tips för att hitta operations

Se `OPERATION_ID_GUIDE.md` för vanliga mönster.

Eller kör:
```powershell
.\List-OperationIds.ps1
```

## 🎯 Nästa steg

1. ✅ Starta servern: `.\Start-McpServer.ps1`
2. ✅ Testa coverage: "Visa test coverage"
3. ⏭️ Lägg till fler tester från andra testfiler
4. ⏭️ Validera capabilities: "Validera getteammembers"

---

**Klart att testa!** 🎉
