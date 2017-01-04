# GameLogWatcher
ゲームのログを Incoming Webhooks を使用して Slack に通知します。

## clone
```
$ git clone --recursive https://github.com/mfakane/GameLogWatcher.git
```

## 設定

Incoming Webhooks (https://slack.com/apps/A0F7XDUAZ-incoming-webhooks) より送信先となる Configuration を作成し、
`config.yml` を編集して Webhook URL や通知条件を設定してください。
