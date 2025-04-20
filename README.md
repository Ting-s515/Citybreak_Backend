<!DOCTYPE html>
<html lang="zh-Hant">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>

<body>
    <h1>Citybreak戶外用品電商網站</h1>
    <h2>目錄 :</h2>
    <ul>
        <li><a href="#project-description">專案說明</a></li>
        <li><a href="#technology-application">技術應用</a></li>
        <li><a href="#database-design">資料庫設計</a></li>
        <li><a href="#function-description">功能說明</a></li>
    </ul>
    <h2 id="project-description">專案說明 : </h2>
    <li>為熱愛戶外活動的消費者提供便捷的電商平台，專注於優質裝備的銷售，滿足各種場景需求。</li>
    <li>前後端分離：後端使用 .NET Core API ，前端使用 Vue.js 架構，達成低耦合的架構，便於日後的擴展和維護。</li>
    <li>前端端連結: <a href="https://github.com/Ting-s515/CityBreak_Frontend">後端程式在這邊</a></li>
    <li>前端程式打包: <a href="https://hub.docker.com/r/tings515/citybreak">點我到docker</a></li>
    <h2 id="technology-application">技術應用 :</h2>
    <ul>
        <li>前端 :
            <ul>
                <li>Vue.js: 使用 Vue Router 管理路徑，並採用 Pinia 管理全域事件</li>
                <li>JS & jQuery</li>
                <li>Bootstrap</li>
            </ul>
        </li>
        <li>後端 :
            <ul>
                <li>.NET API</li>
            </ul>
        </li>
        <li>資料庫 :
            <ul>
                <li>MSSQL</li>
            </ul>
        </li>
        <li>API :
            <ul>
                <li>Google OAuth</li>
            </ul>
        </li>
    </ul>
    <h2 id="database-design">資料庫設計 :</h2>
    <ul>
        <li>保持正規化設計，減少資料重複欄位</li>
        <li>透過建立FK與UQ，維持資料的完整性與唯一性，索引也幫助資料搜尋的效率</li>
        <li>memberTable: 設計userID為PK INENTITY(1,1)，實際上網站會看到的是webMemberID，達到自訂會員編號格式的效果，
            email、phone設定為UQ，避免資料有重複項，PWD則是經由hash加密，保障帳戶安全</li>
        <li>productTable: 有設計一個欄位專門為商品進行分類</li>   
        <li>product_classification: 外鍵參考到ProductTable，一對多關聯</li> 
        <li>orderTable: orderStatus表示訂單付款狀態(未付款、已付款)，merchantTradeNo作為訂單編號</li>
        <li>order_details: 外鍵參考到productTable、orderTable，</li>
    </ul>
    <img src="https://i.imgur.com/Id9vN3b.png" alt="資料庫設計圖" style="max-width: 100%; height: auto;">
    <h2 id="function-description">功能說明 :</h2>
    <ul>
        <li>線上支付功能：串接綠界金融 API，確保支付流程的安全性，提供使用者便捷且安全的交易體驗。</li>
        <li>Google 快速登入：整合 Google OAuth 提供快速登入功能，簡化註冊流程，提升用戶體驗。</li>
    </ul>
</body>

</html>
