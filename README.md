# MMO Server
c# 서버와 유니티를 이용한 2D 존방식 프로젝트


# 환경
* .NET 8.0
* Unity 2022.3.6f2
* ASP.NET
* mssql
* 구글 protobuf 와 EntityFramework 이용


# 서버구조
1. Web Server
   ASp.NET Core로 로그인과 회원가입을 위한 서버
   로그인및 가입후 사용자 데이터를 SharedDB에 업로드 후 게임서버로 접근하는 클라이언트의 정보를 제공한다

2. Game Server
   .NET 프레임워크로 Socket기반 비동기식 서버이다. GoogleProtobuf로 패킷을 관리하며 DB는 MSSQL을 이용해 EntityFrameCore를 이용하였다.


# 게임화면

1. 로그인


<img width="941" height="607" alt="image" src="https://github.com/user-attachments/assets/dc56022e-0d15-4e6a-bbb2-f3679f3a798b" />


2. 서버화면



<img width="886" height="779" alt="image" src="https://github.com/user-attachments/assets/1ed344bc-29bb-4131-9120-0279d7434700" />



3. 게임화면




<img width="1278" height="824" alt="image" src="https://github.com/user-attachments/assets/2149c914-6692-42a9-beb9-5762f42a39cd" />
<img width="1178" height="796" alt="image" src="https://github.com/user-attachments/assets/66261b9a-95fe-46a0-872a-43b16d07cec1" />
