from flask import Flask, jsonify, request,session
import pyodbc
import os
from dotenv import load_dotenv
from flask_cors import CORS
import importlib.util
#檔案路徑
file_path = 'ecpay_payment_sdk.py'
# 檢查檔案是否存在
if not os.path.exists(file_path):
    print("檔案不存在。")
else:
    # 創建模組規範
    print("抓到檔案了")
    spec = importlib.util.spec_from_file_location(
        "ecpay_payment_sdk", file_path)
    module = importlib.util.module_from_spec(spec) # type: ignore
    # 加載模組
    spec.loader.exec_module(module) # type: ignore
from datetime import datetime

# 設定 Flask 實例名稱
app = Flask(__name__, static_folder='static')

# 啟用 CORS
CORS(app, resources={r"/*": {"origins": "*"}})
# 配置隨機密鑰
app.secret_key = os.getenv('SECRET_KEY')

# 載入 .env 檔案
load_dotenv()

# 資料庫連線函數
def sql_connect():
    conn = pyodbc.connect(
        'DRIVER={SQL Server};'
        'SERVER=.\\SQLExpress;'
        'DATABASE=Citybreak;'
        'Trusted_Connection=yes;'
    )
    return conn.cursor()

# 測試資料庫連線
@app.route('/test', methods=['GET'])
def test_db():
    try:
        cursor = sql_connect()
        cursor.execute("SELECT TOP 1 * FROM productTable")
        result = cursor.fetchone()
        if result:
            print("成功連線")
            return jsonify({"success": True, "data": result})
        else:
            return jsonify({"success": False, "message": "No data found"})
    except Exception as e:
        return jsonify({"success": False, "message": str(e)})
    
# 綠界 API 
@app.route('/checkout', methods=['POST'])
def checkout():
    try:
        # 接收前端訂單資料
        data = request.get_json()
        user_id = data.get('userID')
        order_time = data.get('orderTime')
        products = data.get('products', [])
        total_price = data.get('totalPrice')
        # print("前端參數",user_id,order_time)
        if not products or not total_price:
            return jsonify({"success": False, "message": "訂單資料不完整"})

        # 綠界商品名稱組合
        product_name = '#'.join([f"{product['name']} x {product['quantity']}" for product in products])

        # 綠界參數
        order_params = {
            'MerchantTradeNo': datetime.now().strftime("NO%Y%m%d%H%M%S"),
            'MerchantTradeDate': datetime.now().strftime("%Y/%m/%d %H:%M:%S"),
            'PaymentType': 'aio',
            'TotalAmount': int(total_price),
            'TradeDesc': '購物車訂單',
            'ItemName': product_name,
            'ReturnURL': 'https://934c-106-1-120-43.ngrok-free.app/payment/callback',
            'ClientBackURL':'http://localhost:5173/',
            'ChoosePayment': 'ALL',
            'OrderResultURL': '',
            'EncryptType': 1
        }
        # print("order_params= ",order_params)
        # 建立實體
        ecpay_payment_sdk = module.ECPayPaymentSdk(
            MerchantID=os.getenv('MERCHANT_ID'),
            HashKey=os.getenv('HASH_KEY'),
            HashIV=os.getenv('HASH_IV')
        )
        # 產生綠界訂單所需參數
        final_order_params = ecpay_payment_sdk.create_order(order_params)
        # 產生 html 的 form 格式
        action_url = 'https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5'
        html_form = ecpay_payment_sdk.gen_html_post_form(action_url, final_order_params)
        #存進資料庫
        cursor=sql_connect()
        cursor.execute("""
        INSERT INTO orderTable (userID, merchantTradeNo, orderTime, totalPrice, orderStatus)
        VALUES (?, ?, ?, ?, ?)
        """, (user_id, order_params['MerchantTradeNo'], order_time, total_price, '未付款'))
        cursor.commit()
        #找出order_id
        order_row = cursor.execute(""" SELECT orderID FROM orderTable 
            WHERE merchantTradeNo = ? """, (order_params['MerchantTradeNo'],)).fetchone()
        order_id=order_row[0]
        print("check order_id",order_id)
        # 插入 OrderDetails 
        for product in products:
            cursor.execute("""
                INSERT INTO order_details (orderID, productID, quantity)
                VALUES (?, ?, ?)
            """, (order_id, product['productID'], product['quantity']))
        cursor.commit()
        cursor.close()
        # 返回支付表單
        return jsonify({'success': True, 'html': html_form, 'merchantTradeNo':order_params['MerchantTradeNo']})
    except Exception as e:
        return jsonify({'success': False, 'message': str(e)}), 500  

# 接收綠界的交易結果通知
@app.route('/payment/callback', methods=['POST'])
def payment_callback():
    try:
        # 接收綠界的回傳參數
        callback_data = request.form.to_dict()
        print("綠界回傳資料: ", callback_data)
        # 確認交易是否成功
        cursor = sql_connect()
        merchant_trade_no = callback_data.get('MerchantTradeNo')
        print("chack merchant_trade_no=",merchant_trade_no)
        rtn_code = callback_data.get('RtnCode')
        if rtn_code == '1':
            #更新資料表(付款狀態、更新時間)
            cursor.execute("""
                UPDATE orderTable SET orderStatus = ?, latestUpdatedTime = GETDATE()
                WHERE merchantTradeNo = ?
            """, ('已付款', merchant_trade_no))
            print("付款成功！")
            cursor.commit()
            cursor.close()
            return "1|OK"  # 綠界要求成功時需回傳此訊息
        else:
            print("交易失敗！")
            return "0|Fail"
    except Exception as e:
        print("處理綠界回傳時發生錯誤: ", str(e))
        return "0|Fail"
#檢查付款
@app.route('/order/status', methods=['POST'])
def get_order_status():
    data = request.get_json()
    user_id = data.get('userID')
    merchantTradeNo=data.get('merchantTradeNo')
    print("檢查參數",user_id,merchantTradeNo)
    if not user_id:
        return jsonify({'success': False, 'message': 'userID 不存在'})
    if not merchantTradeNo:
        return jsonify({'success': False, 'message': '交易編號不存在'})       

    cursor = sql_connect()
    cursor.execute("""
        SELECT orderStatus FROM orderTable 
        WHERE userID = ? AND merchantTradeNo=?
    """, (user_id,merchantTradeNo))
    result = cursor.fetchone()
    # print("result=",result)
    # print("orderStatus= ",result[0])
    if result:
        return jsonify({'success': True, 'orderStatus': result[0]})
    return jsonify({'success': False, 'message': '找不到訂單'})

if __name__ == '__main__':
    app.run(port=5100, debug=True)