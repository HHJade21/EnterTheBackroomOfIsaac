# Enemy_Siren 애니메이션 설정 가이드

## 개요
`Enemy_Siren`은 다음과 같은 애니메이션 흐름을 가집니다:
1. **Moving** → 이동 중 (Diving 상태로 플레이어에게서 멀어짐)
2. **Disappear** → 잠시 사라짐
3. **Jump** → 나타나며 공격 (원형으로 6발 발사)
4. **Standing** → 공격 후 대기 (약 2초)
5. **Dive** → 다이빙 (Diving 상태 활성화)
6. **Appear** → 나타남
7. **Moving** → 다시 시작 (반복)

## Animator Parameter 설정

### 1. Parameters 탭에서 다음 파라미터 생성:

#### Trigger 타입:
- **`StartAttack`** (Trigger)
  - 용도: Moving → Disappear로 전환할 때 사용

#### Bool 타입:
- **`Move`** (Bool)
  - 용도: Moving 상태를 제어 (true = 이동 중, false = 정지)

## Animator State 설정

### 1. 기본 상태 설정:

#### **Moving** (기본 상태)
- Animation Clip: `Moving`
- Speed: 1.0
- Loop: ✅ 체크
- Transition: 아래 참조

#### **Disappear**
- Animation Clip: `Disappear`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조

#### **Jump**
- Animation Clip: `Jump`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)

#### **Standing**
- Animation Clip: `Standing`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: 애니메이션 길이가 2초여야 함 (또는 Speed 조정으로 2초 재생)

#### **Dive**
- Animation Clip: `Dive`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조
- **중요**: Animation Event 추가 필요 (아래 참조)

#### **Appear**
- Animation Clip: `Appear`
- Speed: 1.0
- Loop: ❌ 체크 해제
- Transition: 아래 참조

## Transition 설정

### 1. Moving → Disappear
- **Conditions:**
  - `StartAttack` (Trigger) = true
  - `Move` (Bool) = false
- **Settings:**
  - Has Exit Time: ❌ 체크 해제
  - Transition Duration: 0.1
  - Interruption Source: None

### 2. Disappear → Jump
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Disappear 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None

### 3. Jump → Standing
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Jump 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None

### 4. Standing → Dive
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Standing 애니메이션이 거의 끝날 때, 약 2초 재생 후)
  - Transition Duration: 0.1
  - Interruption Source: None
- **중요**: Standing 애니메이션이 정확히 2초 재생되도록 설정
  - 방법 1: Standing 애니메이션 클립의 길이를 2초로 설정
  - 방법 2: Standing 애니메이션 클립의 Speed를 조정 (예: 길이가 1초면 Speed = 0.5)

### 5. Dive → Appear
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Dive 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None

### 6. Appear → Moving
- **Conditions:**
  - (조건 없음 - Exit Time만 사용)
- **Settings:**
  - Has Exit Time: ✅ 체크
  - Exit Time: 0.95 (Appear 애니메이션이 거의 끝날 때)
  - Transition Duration: 0.1
  - Interruption Source: None

## Animation Event 설정

### 1. Jump 애니메이션 클립에 이벤트 추가:

1. **Animation 창**에서 `Jump` 클립 선택
2. 공격이 발생해야 할 시점에 이벤트 추가 (예: Jump 애니메이션의 중간 지점)
3. 이벤트 설정:
   - **Function Name:** `OnJumpAttack`
   - **Float Parameter:** (사용 안 함)
   - **Int Parameter:** (사용 안 함)
   - **String Parameter:** (사용 안 함)
   - **Object Reference Parameter:** (사용 안 함)

### 2. Dive 애니메이션 클립에 이벤트 추가:

1. **Animation 창**에서 `Dive` 클립 선택
2. Dive 애니메이션이 시작되는 시점에 이벤트 추가 (예: Dive 애니메이션의 시작 지점)
3. 이벤트 설정:
   - **Function Name:** `OnDiveStart`
   - **Float Parameter:** (사용 안 함)
   - **Int Parameter:** (사용 안 함)
   - **String Parameter:** (사용 안 함)
   - **Object Reference Parameter:** (사용 안 함)

### 3. Jump 애니메이션 클립에 추가 이벤트 (선택사항):

1. **Animation 창**에서 `Jump` 클립 선택
2. Jump 애니메이션이 시작되는 시점에 이벤트 추가
3. 이벤트 설정:
   - **Function Name:** `OnJumpStart`
   - **Float Parameter:** (사용 안 함)
   - **Int Parameter:** (사용 안 함)
   - **String Parameter:** (사용 안 함)
   - **Object Reference Parameter:** (사용 안 함)

## 동작 흐름 설명

### 정상 동작 흐름:
1. **시작**: `Moving` 상태 (Diving으로 플레이어에게서 멀어짐)
2. **목표 거리 도달**: `StartAttack` 트리거 발생 → `Disappear` 상태로 전환
3. **Disappear 완료**: Exit Time에 의해 자동으로 `Jump` 상태로 전환
4. **Jump 중**: Animation Event `OnJumpAttack()` 호출 → 원형으로 6발 발사
5. **Jump 완료**: Exit Time에 의해 자동으로 `Standing` 상태로 전환
6. **Standing 재생**: 약 2초 동안 Standing 애니메이션 재생 (대기)
7. **Standing 완료**: Exit Time에 의해 자동으로 `Dive` 상태로 전환
8. **Dive 시작**: Animation Event `OnDiveStart()` 호출 → Diving 상태 활성화, 콜라이더 전환
9. **Dive 완료**: Exit Time에 의해 자동으로 `Appear` 상태로 전환
10. **Appear 완료**: Exit Time에 의해 자동으로 `Moving` 상태로 전환
11. **반복**: 1번으로 돌아가서 반복

## 주의사항

1. **Animation Event는 반드시 설정해야 합니다**
   - `OnJumpAttack()`: 공격 실행
   - `OnDiveStart()`: Diving 상태 활성화 및 콜라이더 전환
   - `OnJumpStart()` (선택): Diving 상태 비활성화 및 콜라이더 전환

2. **Exit Time 설정**
   - 모든 자동 전환은 Exit Time을 사용하므로, 애니메이션이 거의 끝날 때 전환되도록 설정합니다.
   - Exit Time 값은 0.9~0.95 정도가 적절합니다.

3. **Transition Duration**
   - 너무 길면 애니메이션이 부자연스러울 수 있으므로 0.1 정도로 설정합니다.

4. **Loop 설정**
   - `Moving`만 Loop를 체크하고, 나머지는 모두 체크 해제합니다.

## 문제 해결

### 애니메이션이 멈추는 경우:
- `Moving` 상태로 돌아오지 않는 경우: `Appear → Moving` Transition의 Exit Time 확인
- `Disappear`에서 멈추는 경우: `Disappear → Jump` Transition 확인
- `Standing`에서 멈추는 경우: `Standing → Dive` Transition 확인

### 공격이 발생하지 않는 경우:
- `Jump` 애니메이션 클립에 `OnJumpAttack` 이벤트가 추가되었는지 확인
- 이벤트의 Function Name이 정확히 `OnJumpAttack`인지 확인

### 콜라이더가 전환되지 않는 경우:
- `Dive` 애니메이션 클립에 `OnDiveStart` 이벤트가 추가되었는지 확인
- `Jump` 애니메이션 클립에 `OnJumpStart` 이벤트가 추가되었는지 확인 (선택사항)

