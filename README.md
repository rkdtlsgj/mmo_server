# MMO Server
c# 서버와 유니티를 이용한 2D 존방식 프로젝트


# 환경
* .NET 8.0
* Unity 2022.3.6f2
* ASP.NET
* mssql
* 구글 protobuf 와 EntityFramework 이용

# 개발 목적
* Windows 환경에서 대규모 동시 접속을 안정적으로 처리하기 위해 .NET의 비동기 소켓 기반 네트워크모델과 Google Protocol Buffers를<br>겹합한 MMO 프로젝트 입니다.
* 게임 로직은 Room 단위 싱글 스레드 처리로 레이스 컨디션을 줄이고 브로드캐스트는 zone과 visionCells 방식으로 범위를 제한하여<br>불필요한 네트워크 전송을 줄였습니다.
* DB 작업은 별도의 job으로 처리한 뒤 Room으로 결과를 다시 반영하는 방식으로 구성했습니다.

# 주요 기능
* 비동기 소켓 네트워킹 — Listener의 AcceptAsync + Session의 RecvAsync/SendAsync 완료 콜백 기반 I/O 처리
* ProtoBuf 프로토콜 — Protocol.proto 기반 메시지 타입(C_, S_) 직렬화/역직렬화, MsgId 기반 핸들러 디스패치
* 잡 시스템(JobSerializer) — Room 내부 로직을 단일 실행 흐름으로 직렬화하고, PushAfter로 예약 작업 처리
* 패킷 코드 생성 도구(PacketGenerator) — .proto를 파싱해 PacketManager의 Register 코드 생성(핸들러 테이블 자동화)
* AccountServer 포함 — 별도 웹 서버(ASP.NET) + SharedDB로 계정/인증/연동 흐름 구성
  
# 서버구조
1. Server (게임 서버)
   ClientSession: ProtoBuf 메시지 송신/수신, Ping-Pong, Room 연동<br>
   PacketManager: MsgId → (역직렬화 함수, 핸들러) 테이블 기반 디스패치<br>
   GameLogic / GameRoom: 룸 생성/관리, 룸 단위 로직 실행<br>
   Zone / Map / VisionCube: AOI 계산 및 시야 내 오브젝트 스폰/디스폰/상태 전파

2. DB
   AppDbContext: Accounts/Players/Items 등 DbSet, 인덱스/유니크 제약<br>
   DbTransaction: DB 작업을 JobSerializer로 직렬화하고, 완료 시 Room에 콜백 반영

3. AccountServer / SharedDB
   ASP.NET 기반 계정 서버 + SharedDB 컨텍스트/마이그레이션 포함

# 네트워킹 / 스레드 모델
1. 소켓 I/O 처리 흐름
   Listener.AcceptAsync 완료 → Session 생성 → session.Start(socket)<br>
   Session.RegisterRecv() → Recv 완료 콜백에서 RecvBuffer에 적재<br>
   PacketSession.OnRecv()에서 [2바이트 길이 헤더] 기준으로 패킷 분리<br>
   분리된 패킷 단위로 OnRecvPacket() 호출 → PacketManager 디스패치

2. 게임 로직 동시성 전략
   Room 내부 상태(Player/Monster/Projectile/Zone 등)는 JobSerializer 기반으로 단일 흐름에서 처리<br>
   외부 스레드에서 Room을 건드릴 때는 room.Push(...) 형태로 큐잉하여 레이스 컨디션을 구조적으로 차단

# 프로토콜
* 코드 생성(PacketGenerator)
  Protocol.proto를 읽어 C_*/S_* 메시지 목록을 기반으로<br>
  패킷 등록 코드(매핑 테이블)<br>
  메시지 이름 규칙 정리를 자동 생성하여, 신규 패킷 추가 시 보일러플레이트를 줄이도록 구성했습니다.

# 데이터베이스
* 구성
  AppDbContext에서 Accounts/Players/Items 등을 관리하며, 유니크 인덱스(예: AccountName, PlayerName)를<br>설정해 데이터 무결성을 확보했습니다.
  
* DB 작업 처리 전략
  DB I/O는 DbTransaction이 JobSerializer 기반으로 직렬화하여 처리하고,<br>성공 시 room.Push(() => ...) 형태로 게임 상태(인벤토리 반영 등)를 다시 룸에 안전하게 적용합니다.
  
* AccountServer / SharedDB
  AccountServer는 ASP.NET 기반으로 구성되어 있으며,<br>
  SharedDB 컨텍스트와 마이그레이션을 통해 계정/인증 관련 데이터를 서버와 공유/연동할 수 있는 구조로 구성되어 있습니다.

# 설계포인트
* 비동기 소켓 기반(Windows 내부 IOCP)으로 I/O 완료 콜백 처리
* Room 로직을 JobSerializer로 직렬화하여 동시성 버그를 구조적으로 감소
* Zone + VisionCells(AOI) 로 브로드캐스트 범위를 제한하여 전송 비용 감소
* DB 작업 결과를 room.Push로 반영해 게임 상태 수정의 스레드 안정성 확보
* PacketGenerator로 매핑 테이블 자동화하여 패킷 추가/유지보수 비용 절감

  
# 게임화면
1. 게임화면

<img width="1278" height="824" alt="image" src="https://github.com/user-attachments/assets/2149c914-6692-42a9-beb9-5762f42a39cd" />
<img width="1178" height="796" alt="image" src="https://github.com/user-attachments/assets/66261b9a-95fe-46a0-872a-43b16d07cec1" />
