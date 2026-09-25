# CI (GitHub Actions)

Workflow: `.github/workflows/ci.yml`.

| Job | Roda quando | Precisa |
|---|---|---|
| `typecheck` | todo push/PR | nada |
| `unity-tests` | todo push/PR | secrets de licença Unity (abaixo); sem eles é pulado com aviso |

## Ativar os testes Unity (GameCI)

1. Na sua máquina, com a Unity Hub logada na sua conta, localize o arquivo de licença:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
2. No GitHub: **Settings → Secrets and variables → Actions → New repository secret**:
   - `UNITY_LICENSE` = conteúdo completo do `.ulf`
   - `UNITY_EMAIL` = e-mail da conta Unity
   - `UNITY_PASSWORD` = senha da conta Unity
3. Rode o workflow de novo (aba **Actions → CI → Re-run**).

Referência: https://game-ci.com/docs/github/activation

**Observações**
- A versão da Unity vem de `ProjectSettings/ProjectVersion.txt` (6000.6.2f1). Se o GameCI ainda
  não tiver imagem Docker para essa versão, o job falha no download da imagem; nesse caso a
  alternativa é fixar `unityVersion` numa versão 6000.x com imagem disponível.
- O Tripo3D Bridge (caminho local `D:/...`) é removido do manifest apenas dentro do runner.
