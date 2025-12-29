# Enemy_Priest 애니메이션 설정 가이드

## 개요
`Enemy_Priest`는 다음과 같은 애니메이션 흐름을 가집니다:
1. **Moving** → 이동 애니메이션 (2초 이동, 기본 상태)
2. **AttackStart** → 공격 시작 애니메이션
3. **Attacking** → 공격 중 애니메이션 (5초, 1초마다 원형 탄막 발사)
4. **AttackEnd** → 공격 종료 애니메이션
5. **Standing** → 대기 애니메이션 (1초)
6. **Moving** → 다시 시작 (반복)

## Animator Parameter 설정

### 1. Parameters 탭에서 다음 파라미터 생성:

#### Trigger 타입:
- **`StartAttack`** (Trigger)
  - 용도: Moving → AttackStart로 전환할 때 사용

#### Bool 타입:
- **`Move`** (Bool)
  - 용도: Moving 상태를 제어 (true = 이동 중, false = 정지)
  - 기본값: false

#### Bool 타입:
- **`Attacking`** (Bool)
  - 용도: Attacking 상태를 제어 (true = 공격 중, false = 공격 안 함)
  - 기본값: false

## Animator State 설정

### 1. 기본 상태 설정:

#### **Moving** (기본 상태)
- Animation Clip: `Moving`
- Speed: 1.0
- Loop: ✅ 체크
- Transition: 아래 참조
- **중요**: Entry에서 Moving으로 연결되어야 함

#### **AttackStart**
- Animation Clip: `AttackStart`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)

#### **Attacking**
- Animation Clip: `Attacking`
- Speed: 1.0
- Loop: ✅ 체크 (5초 동안 반복)
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)

#### **AttackEnd**
- Animation Clip: `AttackEnd`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)

#### **Standing**
- Animation Clip: `Standing`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)
- **중요**: 애니메이션 길이가 1초여야 함 (또는 Speed 조정)

## Transition 설정

### 1. Moving → AttackStart
- **Conditions:**
  - `StartAttack` (Trigger) = true
  - `Move` (Bool) = false
- **Settings:**
  - Has Exit Time: ❌ 체크 해제
  - Transition Duration: 0.1
  - Interruption Source: None

### 2. AttackStart → Attacking
- **Conditions:**
  - `Attacking` (Bool) = true
- **Settings:**
  - Has Exit Time: ❌ 체크 해제
  - Transition Duration: 0.1
  - Interruption Source: None

### 3. Attacking → AttackEnd
- **Conditions:**
  - `Attacking` (Bool) = false
- **Settings:**
  - Has Exit Time: ❌ 체크 해제
  - Transition Duration: 0.1
  - Interruption Source: None

### 4. AttackEnd → Standing
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (AttackEnd 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None

### 5. Standing → Moving
- **Conditions:**
  - `Move` (Bool) = true
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Standing 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None


## Animation Event 설정

### 1. AttackStart 애니메이션 클립에 이벤트 추가:

1. **Animation 창**에서 `AttackStart` 클립 선택
2. AttackStart 애니메이션이 끝나는 시점에 이벤트 추가
3. 이벤트 설정:
   - **Function Name:** `OnAttackingStart`
   - **Float Parameter:** (사용 안 함)
   - **Int Parameter:** (사용 안 함)
   - **String Parameter:** (사용 안 함)
   - **Object Reference Parameter:** (사용 안 함)

### 2. Attacking 애니메이션 클립에 이벤트 추가:

**참고**: Attacking 애니메이션이 Loop로 설정되어 있으므로, 애니메이션 이벤트는 사용하지 않습니다.
- `OnAttackingEnd()`는 코루틴에서 5초 후 자동으로 호출됩니다.
- 따라서 Attacking 애니메이션 클립에는 이벤트를 추가하지 않아도 됩니다.

### 3. AttackEnd 애니메이션 클립에 이벤트 추가:

1. **Animation 창**에서 `AttackEnd` 클립 선택
2. AttackEnd 애니메이션이 끝나는 시점에 이벤트 추가
3. 이벤트 설정:
   - **Function Name:** `OnStandingStart`
   - **Float Parameter:** (사용 안 함)
   - **Int Parameter:** (사용 안 함)
   - **String Parameter:** (사용 안 함)
   - **Object Reference Parameter:** (사용 안 함)

### 4. Standing 애니메이션 클립에 이벤트 추가:

1. **Animation 창**에서 `Standing` 클립 선택
2. Standing 애니메이션이 시작되는 시점에 이벤트 추가 (선택사항)
3. 이벤트 설정:
   - **Function Name:** `OnStandingStart`
   - **참고**: 이미 AttackEnd에서 호출되므로 중복 호출 방지 필요

## 동작 흐름 설명

### 정상 동작 흐름:
1. **시작**: `Moving` 상태 (기본 상태, Entry에서 연결)
   - `StartMovement()` 호출로 이동 시작
   - `Move = true` 설정
2. **Moving 재생**: 2초 동안 이동
3. **Moving 완료**: 2초 후 `EndMovement()` 호출
   - `Move` 파라미터를 false로 설정
   - `StartAttackSequence()` 호출
   - `StartAttack` 트리거 발생
4. **Moving → AttackStart**: `StartAttack` 트리거 + `Move = false` 조건으로 전환
5. **AttackStart 완료**: Animation Event `OnAttackingStart()` 호출
   - `Attacking` 파라미터를 true로 설정
   - `AttackingRoutine()` 코루틴 시작 (5초 동안 1초마다 발사)
6. **AttackStart → Attacking**: `Attacking = true` 조건으로 전환
7. **Attacking 재생**: 5초 동안 반복 재생, 1초마다 원형 탄막 발사
8. **Attacking 완료**: 코루틴에서 `OnAttackingEnd()` 호출
   - `Attacking` 파라미터를 false로 설정
   - 공격 코루틴 중지
9. **Attacking → AttackEnd**: `Attacking = false` 조건으로 전환
10. **AttackEnd 완료**: Exit Time에 의해 자동으로 `Standing` 상태로 전환
11. **Standing 시작**: Animation Event `OnStandingStart()` 호출
    - `StandingRoutine()` 코루틴 시작 (1초 대기)
12. **Standing 완료**: 1초 후 `StartMovement()` 호출
    - `Move` 파라미터를 true로 설정
13. **Standing → Moving**: `Move = true` 조건으로 전환
14. **반복**: 2번으로 돌아가서 반복

## 주의사항

1. **Animation Event는 반드시 설정해야 합니다**
   - `OnAttackingStart()`: Attacking 상태 시작 및 공격 코루틴 시작
   - `OnAttackingEnd()`: Attacking 상태 종료 및 공격 코루틴 중지
   - `OnStandingStart()`: Standing 상태 시작 및 대기 코루틴 시작

2. **Attacking 애니메이션은 Loop로 설정**
   - 5초 동안 반복 재생되어야 하므로 Loop를 체크해야 합니다.

3. **Standing 애니메이션 길이**
   - Standing 애니메이션이 정확히 1초 재생되도록 설정:
   - 방법 1: Standing 애니메이션 클립의 길이를 1초로 설정
   - 방법 2: Speed 조정 (예: 길이가 2초면 Speed = 2.0)

4. **Moving 애니메이션은 Loop로 설정**
   - 2초 동안 반복 재생되어야 하므로 Loop를 체크해야 합니다.

5. **Exit Time 설정**
   - AttackEnd → Standing, Standing → Moving은 Exit Time을 사용합니다.
   - Exit Time 값은 0.9~0.95 정도가 적절합니다.

## 문제 해결

### 애니메이션이 멈추는 경우:
- `Moving`에서 멈추는 경우: `Moving → AttackStart` Transition의 조건 확인
- `AttackStart`에서 멈추는 경우: `AttackStart → Attacking` Transition의 조건 확인
- `Attacking`에서 멈추는 경우: `Attacking → AttackEnd` Transition의 조건 확인
- `Standing`에서 멈추는 경우: `Standing → Moving` Transition의 조건 확인

### 공격이 발생하지 않는 경우:
- `AttackStart` 애니메이션 클립에 `OnAttackingStart` 이벤트가 추가되었는지 확인
- `Attacking` 애니메이션 클립에 `OnAttackingEnd` 이벤트가 추가되었는지 확인
- 이벤트의 Function Name이 정확히 일치하는지 확인

### 이동이 시작되지 않는 경우:
- `AttackEnd` 애니메이션 클립에 `OnStandingStart` 이벤트가 추가되었는지 확인
- `Standing → Moving` Transition에 `Move = true` 조건이 있는지 확인
- `StartMovement()` 메서드가 `Move = true`를 설정하는지 확인

