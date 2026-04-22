# 📝 TODO: Saknade API Endpoint-tester

## Status efter mappning

**Tidigare:** 45 mappade tester  
**Nu:** 70 mappade tester  
**Tillagda:** 25 nya mappningar  

## ✅ Nyligen mappade (25 tester)

### Enrollments (7 tester) ✅
- ✅ test_create_enrolment_success
- ✅ test_get_enrolments_consultant
- ✅ test_update_enrolment
- ✅ test_delete_enrolment
- ✅ test_manager_approves_apply_enrolment
- ✅ test_manager_denies_apply_enrolment
- ✅ test_get_pending_applications

### Tags (6 tester) ✅
- ✅ test_create_tag
- ✅ test_update_tag
- ✅ test_delete_tag
- ✅ test_approve_tag
- ✅ test_merge_tags
- ✅ test_filter_courses_by_tag

### Manager Operations (3 tester) ✅
- ✅ test_get_managers
- ✅ test_reassign_consultants
- ✅ test_manager_cannot_edit_other

### Auth & Email (4 tester) ✅
- ✅ test_register
- ✅ test_auth_status
- ✅ test_verify_email_success
- ✅ test_resend_verification

### Bulk Operations (1 test) ✅
- ✅ test_bulk_update_validation

### Images & Utility (4 tester) ✅
- ✅ test_upload_team_member_image
- ✅ test_fallback_image
- ✅ test_contact_form
- ✅ test_security_headers

---

## ⚠️ ENDPOINTS SOM FORTFARANDE SAKNAR TESTER

### 🔴 Prioritet 1 - KRITISKA (7 endpoints)

#### Profile Management
**Endpoints:**
- `GET /api/me/profile` - Hämta egen profil
- `PUT /api/me/profile` - Uppdatera egen profil

**Status:** ❌ INGA TESTER  
**Impact:** HÖG - Används av alla användare  
**Action:** Skapa `test_profile.py` med:
```python
def test_get_own_profile(client, auth_user):
    """User can view their own profile"""

def test_update_own_profile(client, auth_user):
    """User can update their own profile"""

def test_cannot_update_other_profile(client, auth_user):
    """User cannot update another user's profile"""
```

#### Team Toggle
**Endpoint:**
- `PUT /api/team/<id>/toggle` - Aktivera/inaktivera teammedlem

**Status:** ❌ INGA TESTER  
**Impact:** MEDEL - Admin-funktion  
**Action:** Lägg till i `test_team_member_update.py`:
```python
def test_admin_can_toggle_member(client, admin_user):
    """Admin can toggle team member active status"""

def test_non_admin_cannot_toggle(client, regular_user):
    """Non-admin cannot toggle member status"""
```

#### Admin User Verification
**Endpoint:**
- `POST /api/auth/admin/verify-user/<id>` - Admin verifierar användare manuellt

**Status:** ❌ INGA TESTER  
**Impact:** MEDEL - Admin-funktion  
**Action:** Lägg till i `test_admin_rights.py`:
```python
def test_admin_can_verify_user(client, admin_user):
    """Admin can manually verify user email"""
```

#### Password Reset Token View
**Endpoint:**
- `GET /api/auth/reset-password/<token>` - Visa password reset-sida

**Status:** ❌ INGA TESTER  
**Impact:** MEDEL - Vanlig användarfunktion  
**Action:** Lägg till i `test_password_reset.py`:
```python
def test_view_reset_password_page(client):
    """Valid token shows reset password form"""

def test_invalid_token_returns_error(client):
    """Invalid token returns error"""
```

#### Enrollment Data Deletion
**Endpoint:**
- `DELETE /api/consultants/<id>/course-data` - Ta bort all kursdata för konsult

**Status:** ❌ INGA TESTER  
**Impact:** HÖG - GDPR-relaterat  
**Action:** Lägg till i `test_gdpr_offboarding.py`:
```python
def test_delete_consultant_course_data(client, admin_user):
    """Admin can delete all course data for consultant"""

def test_verify_data_actually_deleted(client, admin_user, db):
    """Verify course data is removed from database"""
```

---

### 🟡 Prioritet 2 - VIKTIGA (3 endpoints)

#### Security Reports
**Endpoints:**
- `GET /api/admin/security-reports` - Lista säkerhetsrapporter
- `GET /api/admin/security-reports/latest` - Senaste rapporten

**Status:** ❌ INGA TESTER  
**Impact:** LÅGLÅG - Admin/monitoring  
**Action:** Skapa `test_security_reports.py`:
```python
def test_admin_can_view_security_reports(client, admin_user):
    """Admin can view security reports"""

def test_get_latest_security_report(client, admin_user):
    """Get latest security report"""

def test_non_admin_cannot_access_reports(client, regular_user):
    """Non-admin denied access to security reports"""
```

#### Security Webhook
**Endpoint:**
- `POST /api/security/webhook` - Ta emot säkerhetsnotifieringar

**Status:** ❌ INGA TESTER  
**Impact:** LÅG - Extern integration  
**Action:** Lägg till i `test_security_api.py`:
```python
def test_security_webhook_receives_notification(client):
    """Webhook can receive security notifications"""

def test_webhook_validates_signature(client):
    """Webhook validates request signature"""
```

---

## 📊 Sammanfattning

### Coverage efter uppdatering:

| Kategori | Endpoints | Mappade Tester | Coverage | Status |
|----------|-----------|----------------|----------|--------|
| Auth | 13 | 9 | 69% | 🟡 BRA |
| Team | 6 | 4 | 67% | 🟡 BRA |
| Courses | 5 | 5 | 100% | ✅ KOMPLETT |
| Enrollments | 6 | 6 | 100% | ✅ KOMPLETT |
| Tags | 5 | 5 | 100% | ✅ KOMPLETT |
| Users | 2 | 2 | 100% | ✅ KOMPLETT |
| Security | 3 | 1 | 33% | 🔴 LÅGT |
| Profile | 2 | 0 | 0% | 🔴 SAKNAS |
| Utility | 1 | 1 | 100% | ✅ OK |
| **TOTALT** | **43** | **33** | **~77%** | **🟢 MYCKET BRA** |

### Återstående arbete:

✅ **KOMPLETT:**
- Courses (100%)
- Enrollments (100%)
- Tags (100%)
- Users (100%)

🟡 **BRA men kan förbättras:**
- Auth (69%) - saknar 4 tester
- Team (67%) - saknar 2 tester

🔴 **BEHÖVER ÅTGÄRDAS:**
- Profile (0%) - saknar 2 KRITISKA tester
- Security (33%) - saknar 2 tester
- Team toggle (saknas)

---

## 🎯 Rekommenderad Action Plan

### Sprint 1 - Kritiska luckor (1-2 dagar)
1. ✍️ Skapa `test_profile.py`
   - test_get_own_profile
   - test_update_own_profile
   - test_cannot_update_other_profile

2. ✍️ Uppdatera `test_team_member_update.py`
   - test_admin_can_toggle_member
   - test_non_admin_cannot_toggle

3. ✍️ Uppdatera `test_gdpr_offboarding.py`
   - test_delete_consultant_course_data
   - test_verify_data_actually_deleted

### Sprint 2 - Förbättra täckning (2-3 dagar)
4. ✍️ Uppdatera `test_password_reset.py`
   - test_view_reset_password_page
   - test_invalid_token_returns_error

5. ✍️ Uppdatera `test_admin_rights.py`
   - test_admin_can_verify_user

6. ✍️ Skapa `test_security_reports.py`
   - test_admin_can_view_security_reports
   - test_get_latest_security_report
   - test_non_admin_cannot_access_reports

### Sprint 3 - Komplettering (1 dag)
7. ✍️ Uppdatera `test_security_api.py`
   - test_security_webhook_receives_notification
   - test_webhook_validates_signature

---

## 📈 Förväntad coverage efter TODO:

**Nuvarande:** 77% (33/43 endpoints)  
**Efter Sprint 1:** 88% (38/43 endpoints)  
**Efter Sprint 2:** 95% (41/43 endpoints)  
**Efter Sprint 3:** 100% (43/43 endpoints) ✅

---

## 🔧 Verktyg för att verifiera

Efter att du skrivit nya tester:

```powershell
# Uppdatera TestMappings.json
notepad src\ApiFirst.LlmOrchestration.McpServer\TestMappings.json

# Starta MCP-servern
.\Start-McpServer.ps1

# Använd från LLM
"Visa test coverage"
"Visa capability getprofile"  # efter nya tester
```

---

## 📝 Noteringar

- **Nuvarande 70 tester** täcker de viktigaste användarfallen
- **Saknade 10 tester** är mestadels admin/edge cases
- **Courses, Enrollments, Tags, Users** är 100% täckta ✅
- **Största gap:** Profile management (användarriktade endpoints)
- **Minsta gap:** Security/admin endpoints (låg påverkan på användare)

**Prioritera:** Profile-tester först, sedan team toggle, sedan GDPR data deletion.
