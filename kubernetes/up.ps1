kubectl delete --all pods --namespace=default;`
kubectl delete --all services --namespace=default;`
kubectl delete --all deployments --namespace=default;`


kubectl apply -f .\pod-database.yaml ;`
kubectl apply -f .\cluster-ip-database.yaml ;`
kubectl apply -f .\pod-services.yaml ;`
kubectl apply -f .\cluster-ip-services.yaml ;`
kubectl apply -f .\pod-auth-api.yaml ;`
#kubectl apply -f .\deployment-core-api.yaml ;`
kubectl apply -f .\pod-core-api.yaml ;`
kubectl apply -f .\cluster-ip-apis.yaml ;`
kubectl apply -f .\pod-notification.yaml ;`
kubectl apply -f .\cluster-ip-notification.yaml; `
kubectl apply -f .\pod-web.yaml ;