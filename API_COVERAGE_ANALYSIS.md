# 🔍 API Endpoint Coverage Analysis

## Sammanfattning

**Totalt i backend:** 38 API routes  
**Mappade i TestMappings.json:** 23 operations  
**Coverage:** ~60% (många routes har tester men operations kan vara mappade annorlunda)

## ✅ VAD SOM HAR TESTER

### Authentication (COVERED ✅)
- ✅ POST /api/auth/login → Login
- ✅ POST /api/auth/logout → Logout  
- ✅ POST /api/auth/forgot-password → RequestPasswordReset
- ✅ POST /api/auth/reset-password → ResetPassword
- ✅ GET /api/auth/verify-email/<token> → VerifyEmail

### Team Management (COVERED ✅)
- ✅ GET /api/team → GetTeamMembers
- ✅ PUT /api/team/<id> → UpdateTeamMember
- ✅ DELETE /api/admin/team/<id> → DeleteTeamMember

### Courses (COVERED ✅)
- ✅ POST /api/courses → CreateCourse
- ✅ PATCH /api/courses/<id> → UpdateCourse
- ✅ PATCH /api/courses/<id>/approve → ApproveCourse
- ✅ PATCH /api/courses/<id>/archive → ArchiveCourse
- ✅ DELETE /api/courses/<id> (implicit i mappning)

### Enrollments (COVERED ✅)
- ✅ POST /api/consultants/<id>/courses → EnrollCourse
- ✅ (Implicit GetConsultantCourses från test-namn)

### Users (COVERED ✅)
- ✅ GET /api/users/<id> → GetUser
- ✅ CreateUser, UpdateUser, DeleteUser (från tester)

## ⚠️ VAD SOM SAKNAR TESTER

### Authentication (5 endpoints)
- ❌ POST /api/auth/register
- ❌ POST /api/auth/resend-verification  
- ❌ POST /api/auth/admin/verify-user/<id>
- ❌ GET /api/auth/status
- ❌ GET /api/auth/reset-password/<token>

### Team/Profile (3 endpoints)
- ❌ GET /api/me/profile
- ❌ PUT /api/me/profile
- ❌ PUT /api/team/<id>/toggle
- ❌ PATCH /api/team/bulk

### Enrollments (4 endpoints)
- ❌ DELETE /api/consultants/<id>/course-data
- ❌ DELETE /api/consultants/<id>/courses/<enrolment_id>
- ❌ PATCH /api/consultants/<id>/courses/<enrolment_id>
- ❌ PATCH /api/consultants/<id>/courses/<enrolment_id>/approve
- ❌ DELETE /api/consultants/<id>/courses/<enrolment_id>/deny

### Tags (5 endpoints)
- ❌ POST /api/tags
- ❌ DELETE /api/tags/<id>
- ❌ PATCH /api/tags/<id>
- ❌ PATCH /api/tags/<id>/approve
- ❌ PATCH /api/tags/<id>/merge

### Users (1 endpoint)
- ❌ GET /api/users/managers

### Security/Admin (3 endpoints)
- ❌ GET /api/admin/security-reports
- ❌ GET /api/admin/security-reports/latest
- ❌ POST /api/security/webhook

### Utility (1 endpoint)
- ❌ POST /api/contact

## 📊 Coverage per kategori

| Kategori | Total Routes | Mappade | Coverage |
|----------|-------------|---------|----------|
| Auth | 13 | 5 | 38% |
| Team | 6 | 3 | 50% |
| Courses | 5 | 4 | 80% |
| Enrollments | 6 | 1 | 17% |
| Tags | 5 | 0 | 0% |
| Users | 2 | 1 | 50% |
| Security | 3 | 0 | 0% |
| Utility | 1 | 0 | 0% |
| **TOTALT** | **38** | **~23** | **~60%** |

## 💡 Rekommendationer

### Prioritet 1 - KRITISKA endpoints utan tester:
1. **User registration flow**
   - POST /api/auth/register
   - POST /api/auth/resend-verification

2. **Enrollment management** (mycket använt)
   - PATCH /api/consultants/<id>/courses/<enrolment_id>
   - DELETE /api/consultants/<id>/courses/<enrolment_id>

3. **Profile management**
   - GET /api/me/profile
   - PUT /api/me/profile

### Prioritet 2 - Viktiga men mindre kritiska:
4. **Tags system** (helt otestade)
   - CRUD för tags

5. **Bulk operations**
   - PATCH /api/team/bulk

### Prioritet 3 - Admin/Utility:
6. **Security/Admin endpoints**
7. **Contact form**

## 🔧 Nästa steg

Vill du att jag:

1. **Lägger till mappningar för saknade endpoints** baserat på testfil-namn?
2. **Skapar en lista över vilka tester som behöver skrivas**?
3. **Analyserar befintliga testfiler** för att se om några av dessa endpoints faktiskt testas men inte är mappade?

Kör detta för att se vilka tester som finns för saknade endpoints:

```powershell
# Sök efter enrollment-tester
Select-String -Path "C:\git\InternalAI\backend\tests\test_*.py" -Pattern "enrolment|enrollment" | Select-Object -First 20 Line

# Sök efter tag-tester  
Select-String -Path "C:\git\InternalAI\backend\tests\test_*.py" -Pattern "tag" | Select-Object -First 20 Line

# Sök efter profile-tester
Select-String -Path "C:\git\InternalAI\backend\tests\test_*.py" -Pattern "profile|/me" | Select-Object -First 20 Line
```
