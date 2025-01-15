## Gerador de rotas em realidade aumentada utilizando geolocalização.
### ATENÇÃO: É UM TESTE DE TECNOLOGIA, ENTÃO NÃO CONSIDERE MUITO A ORGANIZAÇÃO. UTILIZE PARA FINS DE ESTUDO.

### Como funciona:
Como é um protótipo, as rotas e áreas de intrusão precisam estar pré alocadas na Unity, no momento da build.
Ao iniciar a aplicação com as rotas, o script irá verificar e validar se o dispositivo possui suporte a VPS (Indispensavel).
Caso afirmativo, irá tentar acessar a localização do GPS que precisa estar ligado.
A rota será plotada em realidade aumentada na tela, no protótipo, uma seta para cada ponto escolhido.
O usuário então poderá seguir a rota plotada em realidade aumentada sendo orientado por geolocalização.
As orientações disponíveis atualmente são:
  - Distância até o ponto final da rota (em metros e em linha reta).
  - Se o usuário está indo na direção correta ou oposta a do ponto final.
  - Se o usuário está dentro de alguma área de detecção de intrusão.
    - A detecção de instrusão é uma área demarcada por geocoordenadas.
Devido a precisão de altitude da API do google, foi adicionado um ajuste fino onde é possivel alterar a altitude (em metros) da rota plotada.

### Sobre o projeto:
- Cada ponto que será plotado na realidade aumentada é referente a uma coordenada de longitude, latitude e altitude.
- O formato esperado é um arquivo .txt na seguinte configuração:
  - LONGITUDE,LATITUDE,ALTITUDE (espaço em branco para separar cada ponto)
    -  (exemplo: -43.001,-19.785,760.551 -43.899,-19.772,760.550 ... )
   
- Os arquivos devem ser posicionados em seus respectivos campos no script VPSManager
  ![image](https://github.com/user-attachments/assets/d72c7dfa-8503-4546-a9db-1f102b94dbb4)

- É OBRIGATORIO SUPORTE A VPS NO DISPOSITIVO E O GPS PRECISA ESTAR LIGADO
- No script VPSManager é possivel alterar os prefabs e imagens para o objeto/imagem que preferir.
- É necessário um token de acesso a API Geospatial do Google (fornecida gratuitamente).
  

### Imagens dos testes de funcionamento:

Ao iniciar> Verifica suporte do VPS  
![image](https://github.com/user-attachments/assets/60815e70-fdfb-4d42-b040-93fb2e5e60ee)

Após, é gerada a rota baseada nas coordenadas fornecidas:  
![image](https://github.com/user-attachments/assets/ab979e47-2b63-4f86-9065-264cacddefcd)

Rota plotada após clicar no botão de plotagem na parte inferior:
É possivel notar no canto inferior direito, um campo para ajuste fino da altitude (em metros) das marcações, devido a precisão de altitude da API do Google.  
![image](https://github.com/user-attachments/assets/a91850c1-ac20-4a73-8f46-3c24c25043b5)

Usuário caminhando para a direção incorreta:  
![image](https://github.com/user-attachments/assets/0bfd50bb-61ec-4a47-9c08-eb5024ad07dd)

Usuário sob zona de intrusão:  
![image](https://github.com/user-attachments/assets/d502e621-98b2-4942-8452-e919752cc582)


